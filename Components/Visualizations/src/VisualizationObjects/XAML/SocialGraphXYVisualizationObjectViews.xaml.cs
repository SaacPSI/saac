// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace SAAC.Visualizations
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using System.Windows.Threading;

    /// <summary>
    /// WPF view (UserControl) for the 2D sociogram visualization.
    /// Keeps original architecture, adds on/off per-layer toggles from VO.
    /// </summary>
    public partial class SocialGraphXYVisualizationObjectViews : UserControl
    {
        // ===== Calques / caches =====
        // --------- Stabilité temporelle (3 nœuds) ----------
        private readonly Dictionary<(double k12, double k13, double k23), Dictionary<string, Point>> _triPoseCache = new();

        // --------- Inclinaison contrôlée (libère Y pour 2 & 3) ----------
        private const double TiltMaxDeg = 18.0; // inclinaison max
        private const double TiltGain = 0.9;  // gain sur l’asymétrie (d13-d12)/(d13+d12)
        private const double BandRatioOfBase = 0.35; // hauteur max autorisée pour 2/3 = 0.35 * base
        private const double BandMinPx = 10.0; // au moins 10 px de bande verticale

        // Arêtes : deux lignes parallèles (haut = SpeechEquality, bas = Gaze)
        private readonly Dictionary<string, Path> edgeSpeech = new();
        private readonly Dictionary<string, Path> edgeGaze = new();
        // Flèches uniquement pour Gaze
        private readonly Dictionary<string, Path> edgeGazeArrow12 = new();
        private readonly Dictionary<string, Path> edgeGazeArrow21 = new();
        // Courbe centrale (utilisée ici avec JVAEvent)
        private readonly Dictionary<string, Path> edgeSyncCurve = new();
        // Capsule de synchronie (filled)
        private readonly Dictionary<string, Path> edgeSynchCapsules = new();

        // Nœuds
        private readonly Dictionary<string, Ellipse> nodeShapes = new();
        private readonly Dictionary<string, TextBlock> nodeLabels = new();
        private readonly Dictionary<string, Ellipse> nodeSurfaces = new();

        // ===== Styles / constantes visuelles =====
        private static readonly Color ColSyncCurve = (Color)ColorConverter.ConvertFromString("#32D0FF");
        private const double SyncThicknessMin = 1.5; // px
        private const double SyncThicknessMax = 3.5; // px
        private const double SyncOpacityMin = 0.35;
        private const double SyncOpacityMax = 0.95;

        private const double CapsuleStrokeMinPx = 0.4; // stroke très fine si sync = 0
        private const double CapsuleStrokeMaxPx = 3.0; // stroke max
        private const double CapsuleMarginPx = 6.0;    // marge pour "englober" le disque
        private const double SyncBulgePx = 15.0;       // bombement de la courbe quadratique

        private const double JVAMaxHalfWidthPx = 9;
        private const double JVAMinHalfWidthPx = 4;

        // Halo sous les nodes
        private const double SurfaceMinScale = 1.15;
        private const double SurfaceMaxScale = 3.0;
        private const double SurfaceYOffsetFactor = 0.12;
        private const double SurfaceOpacity = 0.22;

        // Arêtes parallèles
        private const double PairEdgeSeparationPx = 10.0;

        // Flèches Gaze
        private const double ArrowMinPx = 4.0;
        private const double ArrowMaxPx = 15.0;

        // Couleurs utilitaires
        private static readonly Color ColRed = (Color)ColorConverter.ConvertFromString("#ff0000");
        private static readonly Color ColGreen = (Color)ColorConverter.ConvertFromString("#2ecc71");

        // ===== Monde =====
        private readonly Dictionary<string, Point> posM = new();
        private readonly Dictionary<string, Vector> velM = new();

        // Physique de suivi
        private const double NaturalFreq = 3.0;
        private const double Damping = 1.0;
        private const double MaxSpeed = 0.8;

        // ===== Légendes =====
        private Border _legend;
        private Border _groupLegend;

        // Redraw coalescing
        private bool _redrawPending;

        // Signature de triangle pour stabilité temporelle (d12,d13,d23 en mètres d'affichage)
        private (int k12, int k13, int k23)? _lastTriSignature;

        // Seuils pour décider de snap/inertie
        private const double TinySignatureEpsMeters = 1e-4;  // ~0.1 mm : identique
        private const double BigJumpSumMeters = 0.25;   // ~25 cm : seek/jump

        // --- Contraintes d’ordre (pas de “2 au-dessus de 1”, etc.) ---
        private const double OrderMarginMinPx = 8.0; // marge min (pixels) entre 1 et {2,3}


        public SocialGraphXYVisualizationObjectViews()
        {
            InitializeComponent();

            this.Loaded += this.OnLoaded;
            this.Unloaded += this.OnUnloaded;
            this.SizeChanged += this.OnSizeChanged;
            this.DataContextChanged += this.OnDataContextChanged;

            this.BuildGeneralLegend();
        }

        private SocialGraphXYVisualizationObject VO => this.DataContext as SocialGraphXYVisualizationObject;

        private static double Clamp(double v, double min, double max) => v < min ? min : (v > max ? max : v);
        private static double Clamp01(double v) => Math.Max(0, Math.Min(1, v));
        private static string MakeKey(string a, string b) => string.CompareOrdinal(a, b) <= 0 ? $"{a}|{b}" : $"{b}|{a}";

        private static (string a, string b) GetPair(PersonEdge e)
        {
            var ids = new[] { e.FromId_1, e.ToId_1, e.FromId_2, e.ToId_2 }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();
            if (ids.Count >= 2) return (ids[0], ids[1]);

            var a = e.FromId_1 ?? e.ToId_1 ?? e.FromId_2 ?? e.ToId_2 ?? string.Empty;
            var b = e.ToId_1 ?? e.ToId_2 ?? e.FromId_1 ?? e.FromId_2 ?? string.Empty;
            return string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);
        }

        private static Point ToPx(Point pMeters, double pxPerM) => new(pMeters.X * pxPerM, pMeters.Y * pxPerM);
        private static double Lerp(double a, double b, double t) => a + (b - a) * Math.Max(0, Math.Min(1, t));

        // ===== Redraw coalescing =====
        private void RequestRedraw()
        {
            if (this._redrawPending) return;
            this._redrawPending = true;
            this.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
            {
                this._redrawPending = false;
                this.Redraw();
            }));
        }

        private void OnLoaded(object sender, RoutedEventArgs e) => this.RequestRedraw();

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (this.VO is INotifyPropertyChanged npc)
                npc.PropertyChanged -= this.OnVOChanged;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) => this.RequestRedraw();

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is INotifyPropertyChanged oldNpc)
                oldNpc.PropertyChanged -= this.OnVOChanged;
            if (e.NewValue is INotifyPropertyChanged newNpc)
                newNpc.PropertyChanged += this.OnVOChanged;

            this.RequestRedraw();
        }

        private void OnVOChanged(object s, PropertyChangedEventArgs e)
        {
            // Re-dessiner dès que les flags / échelles changent
            if (e.PropertyName == nameof(SocialGraphXYVisualizationObject.CurrentValue) ||
                e.PropertyName == nameof(SocialGraphXYVisualizationObject.ShowLabels) ||
                e.PropertyName == nameof(SocialGraphXYVisualizationObject.ShowNodesSpeakingTime) ||
                e.PropertyName == nameof(SocialGraphXYVisualizationObject.ShowEdgesGaze) ||
                e.PropertyName == nameof(SocialGraphXYVisualizationObject.ShowEdgesSynchrony) ||
                e.PropertyName == nameof(SocialGraphXYVisualizationObject.ShowEdgesJVA) ||
                e.PropertyName == nameof(SocialGraphXYVisualizationObject.ShowEdgesSpeechEquality) ||
                e.PropertyName == nameof(SocialGraphXYVisualizationObject.DisplayMinDistanceMeters) ||
                e.PropertyName == nameof(SocialGraphXYVisualizationObject.DistanceGainK) ||
                e.PropertyName == nameof(SocialGraphXYVisualizationObject.MetersPerPixel))
            {
                if (!this.Dispatcher.CheckAccess())
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => this.OnVOChanged(s, e)));
                    return;
                }
                this.RequestRedraw();
            }
        }

        private static Brush MakeSurfaceBrush(Color baseColor)
        {
            var br = new RadialGradientBrush
            {
                Center = new Point(0.5, 0.5),
                GradientOrigin = new Point(0.5, 0.5),
                RadiusX = 0.5,
                RadiusY = 0.5
            };
            var cInner = Color.FromArgb(120, baseColor.R, baseColor.G, baseColor.B);
            var cOuter = Color.FromArgb(0, baseColor.R, baseColor.G, baseColor.B);

            br.GradientStops.Add(new GradientStop(cInner, 0.0));
            br.GradientStops.Add(new GradientStop(cOuter, 1.0));
            br.Freeze();
            return br;
        }

        /// <summary>Main render.</summary>
        private void Redraw()
        {
            var snap = this.VO?.CurrentValue;
            if (snap == null) return;

            var nodes = snap.Value.Data.PersonNodes ?? new List<PersonNode>();
            var edges = snap.Value.Data.PersonEdges ?? new List<PersonEdge>();

            // Flags d’affichage (depuis le VO)
            bool showNodeSize = this.VO?.ShouldShowNodeSpeakingTime() ?? true;
            bool showGaze = this.VO?.ShouldShowEdgeGaze() ?? true;
            bool showSync = this.VO?.ShouldShowEdgeSynchrony() ?? true;   // Capsule
            bool showJva = this.VO?.ShouldShowEdgeJVA() ?? true;         // Courbe centrale (JVAEvent dans ce code)
            bool showSpeechEq = this.VO?.ShouldShowEdgeSpeechEquality() ?? true;

            this.EnsureNodeVisuals(nodes);
            this.EnsureEdgeVisuals(edges);

            // Cibles déterministes avec inclinaison et contraintes d’ordre
            Dictionary<string, Point> targetsM;
            if (nodes.Count == 3)
            {
                // IDs canoniques
                var id1 = nodes.FirstOrDefault(n => n.Id == "1")?.Id ?? nodes[0].Id;
                var id2 = nodes.FirstOrDefault(n => n.Id == "2")?.Id ?? nodes[1].Id;
                var id3 = nodes.FirstOrDefault(n => n.Id == "3")?.Id ?? nodes[2].Id;

                // Distances d’affichage (m)
                double d12 = MapProximityMetersToDisplayDistance(GetProximityMeters(id1, id2, edges, 1.0));
                double d13 = MapProximityMetersToDisplayDistance(GetProximityMeters(id1, id3, edges, 1.0));
                double d23 = MapProximityMetersToDisplayDistance(GetProximityMeters(id2, id3, edges, 1.0));
                EnforceTriangleInequality(ref d12, ref d13, ref d23);

                // Signature quantisée (mm)
                var key = MakeTriKey(d12, d13, d23);

                
                bool cacheHit = _triPoseCache.TryGetValue(key, out var cached);
                if (cacheHit)
                {
                    targetsM = cached;
                }
                else
                {
                    targetsM = this.ComputeTriangleWithTiltOrdered(nodes, (d12, d13, d23));
                    _triPoseCache[key] = targetsM; // même pose à l’aller/retour
                }

                // SNAP si revisite/gros seek, sinon inertie fluide à chaque update
                bool snapNow = ShouldSnapToTargets(
                    key, _lastTriSignature,
                    (d12, d13, d23),
                    _lastTriSignature.HasValue
                        ? (((double)_lastTriSignature.Value.k12 / 1000.0),
                           ((double)_lastTriSignature.Value.k13 / 1000.0),
                           ((double)_lastTriSignature.Value.k23 / 1000.0))
                        : (double.NaN, double.NaN, double.NaN),
                    cacheHit);

                _lastTriSignature = key;

                if (snapNow)
                {
                    foreach (var n in nodes)
                    {
                        this.posM[n.Id] = targetsM[n.Id];
                        this.velM[n.Id] = new Vector();
                    }
                }
                else
                {
                    this.InertialFollow(targetsM, nodes);
                }
            }
            else
            {
                // N != 3 : layout + inertie existants
                targetsM = this.ComputeTargetsMeters(nodes, edges);
                this.InertialFollow(targetsM, nodes);
            }



            // m -> px
            double mPerPx = this.VO.MetersPerPixel;
            double pxPerM = (mPerPx > 0) ? 1.0 / mPerPx : 100.0;

            // Normalisation "douce" pour Gaze (si aucune valeur, évite /0)
            double gazeMax = Math.Max(1.0, edges.Select(ed => ed.GazeOnPeers12 + ed.GazeOnPeers21).DefaultIfEmpty(0).Max());

            foreach (var e in edges)
            {
                var (a, b) = GetPair(e);
                if (!this.posM.ContainsKey(a) || !this.posM.ContainsKey(b)) continue;

                var pa = ToPx(this.posM[a], pxPerM);
                var pb = ToPx(this.posM[b], pxPerM);
                var key = MakeKey(a, b);

                // Rayons (px) pour rognage
                var na = nodes.FirstOrDefault(n => n.Id == a);
                var nb = nodes.FirstOrDefault(n => n.Id == b);
                double rA = na != null ? this.VO.GetNodeRadius(na) : 0;
                double rB = nb != null ? this.VO.GetNodeRadius(nb) : 0;
                double trimA = rA + 4.0, trimB = rB + 4.0;

                // ===== SPEECH (haut) =====
                {
                    var speechStyle = MapSpeechStyle(e.SpeechEquality);
                    ComputeOffsetSegment(pa, pb, +PairEdgeSeparationPx * 0.5, trimA, trimB, out var sA, out var sB);

                    var sGeom = new StreamGeometry();
                    using (var sc = sGeom.Open()) { sc.BeginFigure(sA, false, false); sc.LineTo(sB, true, false); }
                    sGeom.Freeze();

                    var pSpeech = this.edgeSpeech[key];
                    pSpeech.Data = sGeom;
                    pSpeech.Stroke = speechStyle.Stroke;
                    pSpeech.StrokeThickness = speechStyle.Thickness;
                    pSpeech.StrokeDashArray = speechStyle.Dash;
                    pSpeech.Visibility = showSpeechEq ? Visibility.Visible : Visibility.Collapsed;
                }

                // ===== GAZE (bas) + flèches =====
                {
                    double g12 = Math.Max(0, e.GazeOnPeers12);
                    double g21 = Math.Max(0, e.GazeOnPeers21);
                    double gSum = g12 + g21;
                    double gaze01 = gSum > 0 ? gSum / gazeMax : 0.0;

                    double gazeThickness = 1.0 + 3 * Clamp01(gaze01);
                    var gazeBrush = new SolidColorBrush(ColGreen);

                    ComputeOffsetSegment(pa, pb, -PairEdgeSeparationPx * 0.5, trimA, trimB, out var gA, out var gB);

                    var gGeom = new StreamGeometry();
                    using (var gc = gGeom.Open()) { gc.BeginFigure(gA, false, false); gc.LineTo(gB, true, false); }
                    gGeom.Freeze();

                    var pGaze = this.edgeGaze[key];
                    pGaze.Data = gGeom;
                    pGaze.Stroke = gazeBrush;
                    pGaze.StrokeThickness = gazeThickness;
                    pGaze.StrokeDashArray = (gSum <= 0) ? new DoubleCollection { 3, 4 } : null;
                    pGaze.Visibility = showGaze ? Visibility.Visible : Visibility.Collapsed;

                    var a12Path = this.edgeGazeArrow12[key];
                    var a21Path = this.edgeGazeArrow21[key];

                    if (showGaze && g12 > 0)
                    {
                        double t12 = (g12 >= g21 ? 1.0 : (g21 > 0 ? g12 / g21 : 1.0));
                        double size12 = ArrowMinPx + Clamp01(t12) * (ArrowMaxPx - ArrowMinPx);
                        a12Path.Data = MakeArrow(gA, gB, size12);
                        a12Path.Fill = gazeBrush;
                        a12Path.Visibility = Visibility.Visible;
                    }
                    else a12Path.Visibility = Visibility.Collapsed;

                    if (showGaze && g21 > 0)
                    {
                        double t21 = (g21 >= g12 ? 1.0 : (g12 > 0 ? g21 / g12 : 1.0));
                        double size21 = ArrowMinPx + Clamp01(t21) * (ArrowMaxPx - ArrowMinPx);
                        a21Path.Data = MakeArrow(gB, gA, size21);
                        a21Path.Fill = gazeBrush;
                        a21Path.Visibility = Visibility.Visible;
                    }
                    else a21Path.Visibility = Visibility.Collapsed;
                }

                // ===== COURBE CENTRALE (ici reliée à JVAEvent dans ce code) =====
                if (this.edgeSyncCurve.TryGetValue(key, out var syncPath))
                {
                    double y = Clamp01(e.JVAEvent); // on garde le comportement en place

                    var geom = MakeQuadraticCurve(pa, pb, SyncBulgePx, trimA, trimB);
                    syncPath.Data = geom;

                    syncPath.StrokeThickness = SyncThicknessMin + y * (SyncThicknessMax - SyncThicknessMin);
                    syncPath.Opacity = SyncOpacityMin + y * (SyncOpacityMax - SyncOpacityMin);
                    syncPath.Visibility = showJva ? Visibility.Visible : Visibility.Collapsed;
                }

                // ===== CAPSULE SYNCHRONY (filled) =====
                {
                    var cap = this.edgeSynchCapsules[key];
                    double s = Clamp01(e.Synchrony);

                    double engulfHalfWidthPx = Math.Max(JVAMaxHalfWidthPx, Math.Max(rA, rB) + CapsuleMarginPx);
                    double minHalfWidthPx = Math.Max(1.0, JVAMinHalfWidthPx);
                    double halfWidthPx = Lerp(minHalfWidthPx, engulfHalfWidthPx, s);

                    cap.Data = MakeCapsule(pa, pb, halfWidthPx);
                    cap.StrokeThickness = Lerp(CapsuleStrokeMinPx, CapsuleStrokeMaxPx, s);
                    cap.Stroke = Brushes.Pink;
                    cap.Fill = Brushes.Pink;
                    cap.Opacity = Lerp(0.06, 0.18, s);
                    cap.Visibility = showSync ? Visibility.Visible : Visibility.Collapsed;
                    Panel.SetZIndex(cap, 1);
                }
            }

            // ===== Nodes =====
            foreach (var n in nodes)
            {
                double r = this.VO.GetNodeRadius(n); // déjà géré par ShowNodesSpeakingTime côté VO
                var ppx = ToPx(this.posM[n.Id], pxPerM);

                var ell = this.nodeShapes[n.Id];
                ell.Width = ell.Height = 2 * r;
                Canvas.SetLeft(ell, ppx.X - r);
                Canvas.SetTop(ell, ppx.Y - r);
                ell.Stroke = Brushes.White; // inchangé

                // Surface (halo) sous le node
                if (this.nodeSurfaces.TryGetValue(n.Id, out var surf))
                {
                    double t = Clamp01(n.TaskParticipation);
                    double scale = SurfaceMinScale + t * (SurfaceMaxScale - SurfaceMinScale);
                    double rs = r * scale;
                    double yOffset = r * SurfaceYOffsetFactor;

                    surf.Width = surf.Height = 2 * rs;
                    Canvas.SetLeft(surf, ppx.X - rs);
                    Canvas.SetTop(surf, ppx.Y - rs + yOffset);

                    if (surf.Fill == null || surf.Fill.IsFrozen)
                        surf.Fill = MakeSurfaceBrush(GetColorId(int.Parse(n.Id)));
                    surf.Opacity = SurfaceOpacity;
                    Panel.SetZIndex(surf, 5);
                }

                var tb = this.nodeLabels[n.Id];
                tb.Text = n.Id;
                tb.Visibility = this.VO.ShowLabels ? Visibility.Visible : Visibility.Collapsed;

                tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(tb, ppx.X - tb.DesiredSize.Width / 2.0);
                Canvas.SetTop(tb, ppx.Y - r - tb.FontSize - 2);

                Panel.SetZIndex(ell, 10);
                Panel.SetZIndex(tb, 11);
            }
            this.BuildGeneralLegend(showGaze, showSync, showJva, showSpeechEq);
            this.BuildGroupLegend(snap.Value.Data.PersonGroup);
            this.UpdateGeneralLegendPosition();
            this.UpdateGroupLegendPosition();
        }

        // ===== Géométries =====

        // Capsule (ruban) d'épaisseur 2*w autour du segment AB
        private static Geometry MakeCapsule(Point a, Point b, double w)
        {
            var v = b - a;
            var len = v.Length;
            if (len < 1) len = 1;
            v.Normalize();
            var n = new Vector(-v.Y, v.X) * w;

            var a1 = a + n;
            var a2 = a - n;
            var b1 = b + n;
            var b2 = b - n;

            var g = new StreamGeometry();
            using var ctx = g.Open();
            ctx.BeginFigure(a1, true, true);
            ctx.LineTo(b1, true, false);
            ctx.ArcTo(b2, new Size(w, w), 180, false, SweepDirection.Counterclockwise, true, true);
            ctx.LineTo(a2, true, false);
            ctx.ArcTo(a1, new Size(w, w), 180, false, SweepDirection.Counterclockwise, true, true);
            g.Freeze();
            return g;
        }

        // 3 nœuds : 1 en haut, 2 en bas-gauche, 3 en bas-droite.
        // Les nœuds 2 et 3 peuvent monter/descendre mais on impose : y1 < y2 - marge et y1 < y3 - marge,
        // et x2 < x3 (pas d'inversion). On ajuste l'inclinaison par dichotomie pour respecter ces contraintes.
        private Dictionary<string, Point> ComputeTriangleWithTiltOrdered(
            List<PersonNode> nodes, (double d12, double d13, double d23) d)
        {
            if (nodes.Count != 3) throw new InvalidOperationException("Attendu exactement 3 nœuds.");

            PersonNode n1 = nodes.FirstOrDefault(n => n.Id == "1") ?? nodes[0];
            PersonNode n2 = nodes.FirstOrDefault(n => n.Id == "2") ?? nodes[1];
            PersonNode n3 = nodes.FirstOrDefault(n => n.Id == "3") ?? nodes[2];

            double d12 = d.d12, d13 = d.d13, d23 = d.d23;
            EnforceTriangleInequality(ref d12, ref d13, ref d23);

            // --- Construction canonique (base horizontale 2-3) ---
            const double tiny = 1e-6;
            if (d23 < tiny) d23 = tiny;
            double half = d23 * 0.5;
            var p2 = new Point(-half, 0.0);
            var p3 = new Point(+half, 0.0);

            // Coordonnées de 1 (y négatif = "haut" en WPF)
            double x1 = (d12 * d12 - d13 * d13) / (2.0 * d23);
            double under = Math.Max(d12 * d12 - x1 * x1, 0.0);
            double y1 = -Math.Sqrt(under);

            // Altitude minimale (évite triangle plat)
            double pxPerM = 1.0 / Math.Max(this.VO?.MetersPerPixel ?? 0.01, 1e-6);
            double minAltM = Math.Max(12.0 / pxPerM, 0.18 * d23);
            if (Math.Abs(y1) < minAltM) y1 = -minAltM;

            var p1 = new Point(x1, y1);

            // Centroïde initial
            var g0 = new Point((p1.X + p2.X + p3.X) / 3.0, (p1.Y + p2.Y + p3.Y) / 3.0);

            // Inclinaison cible depuis l'asymétrie (d13 - d12)
            double asym = (d13 - d12) / Math.Max(d12 + d13, 1e-6);
            double phiTarget = (TiltMaxDeg * Math.PI / 180.0) * TiltGain * asym;
            // borne dure
            phiTarget = Math.Max(-TiltMaxDeg * Math.PI / 180.0, Math.Min(TiltMaxDeg * Math.PI / 180.0, phiTarget));

            // Marge d'ordre (m) entre 1 et {2,3}
            double orderMarginM = Math.Max(OrderMarginMinPx / pxPerM, 0.10 * d23);

            // Fonction de test des contraintes pour un angle phi
            bool Satisfies(double phi)
            {
                var P1 = RotateAround(p1, g0, phi);
                var P2 = RotateAround(p2, g0, phi);
                var P3 = RotateAround(p3, g0, phi);
                // 1 doit rester au-dessus de 2 et 3
                if (!(P1.Y <= P2.Y - orderMarginM && P1.Y <= P3.Y - orderMarginM)) return false;
                // 2 doit rester à gauche de 3 (petit epsilon)
                if (!(P2.X <= P3.X - 1e-9)) return false;
                return true;
            }

            // Si la cible ne respecte pas l’ordre, on réduit |phi| par dichotomie
            double sign = Math.Sign(phiTarget);
            double lo = 0.0, hi = Math.Abs(phiTarget);
            if (!Satisfies(sign * hi))
            {
                for (int it = 0; it < 20; it++)
                {
                    double mid = 0.5 * (lo + hi);
                    if (Satisfies(sign * mid)) lo = mid; else hi = mid;
                }
                phiTarget = sign * lo;
            }

            // Applique l’inclinaison retenue
            var p1r = RotateAround(p1, g0, phiTarget);
            var p2r = RotateAround(p2, g0, phiTarget);
            var p3r = RotateAround(p3, g0, phiTarget);

            // Recentre le centroïde au milieu de la vue (en mètres)
            double cxm = (this.Scene.ActualWidth * 0.5) / pxPerM;
            double cym = (this.Scene.ActualHeight * 0.5) / pxPerM;
            var g = new Point((p1r.X + p2r.X + p3r.X) / 3.0, (p1r.Y + p2r.Y + p3r.Y) / 3.0);
            var shift = new Vector(cxm - g.X, cym - g.Y);

            return new Dictionary<string, Point>(StringComparer.Ordinal)
            {
                [n1.Id] = new Point(p1r.X + shift.X, p1r.Y + shift.Y), // 1 en haut
                [n2.Id] = new Point(p2r.X + shift.X, p2r.Y + shift.Y), // 2 bas-gauche
                [n3.Id] = new Point(p3r.X + shift.X, p3r.Y + shift.Y), // 3 bas-droite
            };
        }


        // Segment décalé + rogné (évite d’entrer dans les disques)
        private static void ComputeOffsetSegment(Point a, Point b, double offsetPx,
                                                 double trimStartPx, double trimEndPx,
                                                 out Point aOff, out Point bOff)
        {
            var v = b - a; var len = v.Length;
            if (len < 1e-6) { aOff = a; bOff = b; return; }
            var dir = v / len;
            var n = new Vector(-dir.Y, dir.X);
            var off = n * offsetPx;

            var aTrim = a + dir * trimStartPx;
            var bTrim = b - dir * trimEndPx;

            aOff = aTrim + off;
            bOff = bTrim + off;
        }

        // Courbe quadratique entre A et B, bombée de 'bulgePx', rognée aux bords des nœuds
        private static Geometry MakeQuadraticCurve(Point a, Point b, double bulgePx, double trimStartPx, double trimEndPx)
        {
            var v = b - a; var len = v.Length;
            if (len < 1.0)
            {
                var g0 = new StreamGeometry();
                using var c0 = g0.Open();
                c0.BeginFigure(a, false, false);
                c0.LineTo(b, true, false);
                g0.Freeze();
                return g0;
            }

            var dir = v / len;
            var aTrim = a + dir * trimStartPx;
            var bTrim = b - dir * trimEndPx;

            var m = new Point((aTrim.X + bTrim.X) * 0.5, (aTrim.Y + bTrim.Y) * 0.5);
            var n = new Vector(-dir.Y, dir.X);
            var ctrl = new Point(m.X + n.X * bulgePx, m.Y + n.Y * bulgePx);

            var g = new StreamGeometry();
            using var ctx = g.Open();
            ctx.BeginFigure(aTrim, false, false);
            ctx.QuadraticBezierTo(ctrl, bTrim, true, false);
            g.Freeze();
            return g;
        }

        // Flèche (start -> end)
        private static Geometry MakeArrow(Point start, Point end, double baseSize)
        {
            var dir = end - start; var len = dir.Length;
            if (len < 0.001) return Geometry.Empty;
            dir /= len;
            var n = new Vector(-dir.Y, dir.X);

            double s = baseSize;
            var tip = end;
            var p1 = tip - dir * s + n * (s * 0.5);
            var p2 = tip - dir * s - n * (s * 0.5);

            var g = new StreamGeometry();
            using var ctx = g.Open();
            ctx.BeginFigure(tip, true, true);
            ctx.LineTo(p1, true, false);
            ctx.LineTo(p2, true, false);
            g.Freeze();
            return g;
        }

        // ===== Styles =====
        private (Brush Stroke, double Thickness, DoubleCollection Dash) MapSpeechStyle(double equality01)
        {
            Brush color;
            double value;
            if (equality01 == -1)
            {
                color = new SolidColorBrush(ColRed);
                value = 0.0;
            }
            else
            {
                color = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f39c12"));
                value = 1 - Clamp01(equality01);
            }
            double t = 1.0 + 2.0 * value;
            DoubleCollection dash = value < 0.4 ? new DoubleCollection { 1.5, 5 } : null;
            return (color, t, dash);
        }

        // ===== Helpers couleurs =====
        private static Color Interpolate(Color a, Color b, double t)
        {
            t = Clamp01(t);
            byte LerpB(byte x, byte y) => (byte)Math.Round(x + (y - x) * t);
            return Color.FromArgb(LerpB(a.A, b.A), LerpB(a.R, b.R), LerpB(a.G, b.G), LerpB(a.B, b.B));
        }

        private Color GetColorId(int id)
        {
            return id switch
            {
                1 => Color.FromRgb(239, 247, 0),
                2 => Color.FromRgb(0, 206, 7),
                3 => Color.FromRgb(196, 0, 255),
                _ => Color.FromRgb(0, 0, 0),
            };
        }

        // ===== Inertie des nœuds =====
        private DateTime _lastTick = DateTime.MinValue;
        private double GetDeltaTime()
        {
            var now = DateTime.UtcNow;
            if (_lastTick == DateTime.MinValue) { _lastTick = now; return 1.0 / 60.0; }
            var dt = (now - _lastTick).TotalSeconds;
            _lastTick = now;
            return Clamp(dt, 1.0 / 120.0, 1.0 / 15.0);
        }

        private void InertialFollow(Dictionary<string, Point> targetsM, IEnumerable<PersonNode> nodes)
        {
            double dt = GetDeltaTime();

            foreach (var n in nodes)
            {
                var id = n.Id;

                if (!this.posM.TryGetValue(id, out var p))
                {
                    this.posM[id] = targetsM[id];
                    this.velM[id] = new Vector(0, 0);
                    continue;
                }
                if (!this.velM.TryGetValue(id, out var v))
                    v = new Vector(0, 0);

                var to = targetsM[id] - p;
                double wn = NaturalFreq;
                double z = Damping;
                var a = (wn * wn) * to - 2.0 * z * wn * v;

                v += a * dt;

                var sp = v.Length;
                if (sp > MaxSpeed) v *= (MaxSpeed / sp);

                if (to.Length < this.VO.DeadbandMeters) v = new Vector(0, 0);

                p += v * dt;

                this.posM[id] = p;
                this.velM[id] = v;
            }
        }

        // ===== Légendes =====
        private void BuildGeneralLegend(bool showGaze = true, bool showSynchrony = true, bool showJVA = true, bool showSpeechEquality = true)
        {
            //if (this._legend != null) return;

            Path SampleLine(Brush b) => new()
            {
                Stroke = b,
                StrokeThickness = 3,
                Data = Geometry.Parse("M0,0 L40,0"),
                Margin = new Thickness(0, 0, 6, 0),
                IsHitTestVisible = false
            };
            var lineGaze = SampleLine(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ecc71")));
            var lineSync = SampleLine(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498db")));
            var lineSpeech = SampleLine(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f39c12")));
            var jvaCap = new Rectangle
            {
                Width = 40,
                Height = 10,
                Fill = Brushes.Pink,
                Opacity = 0.22,
                Margin = new Thickness(0, 0, 6, 0),
                IsHitTestVisible = false
            };
            var nodeSample = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = Brushes.Gray,
                Stroke = Brushes.White,
                StrokeThickness = 1,
                Margin = new Thickness(0, 0, 6, 0),
                IsHitTestVisible = false
            };

            StackPanel Row(UIElement icon, string text) => new()
            {
                Orientation = Orientation.Horizontal,
                Children = { icon, new TextBlock { Text = text, Foreground = Brushes.White, FontSize = 12 } },
                Margin = new Thickness(0, 2, 0, 2),
                IsHitTestVisible = false
            };
            List<StackPanel> stackPanels = new List<StackPanel>();
            if (showSpeechEquality) stackPanels.Add(Row(lineSpeech, "Speech Equality (thick = high equality)"));
            if (showGaze) stackPanels.Add(Row(lineGaze, "Gaze On Peers (thick = high GoP, arrows show gazed)"));
            if (showJVA) stackPanels.Add(Row(lineSync, "JVA (thick = high JVA)"));
            if (showSynchrony) stackPanels.Add(Row(jvaCap, "Synchrony = (thick = High synchrony)"));


            var sp = new StackPanel
            {
                Orientation = Orientation.Vertical,
                IsHitTestVisible = false
            };
            sp.Children.Add(Row(nodeSample, "Individual (size = speaking time)"));
            foreach (var row in stackPanels)
            {
                sp.Children.Add(row);
            }

            if (this._legend != null)
            {
                this.Scene.Children.Remove(this._legend);
                this._legend = null;
            }

            this._legend = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(160, 20, 20, 20)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8),
                Child = sp,
                IsHitTestVisible = false
            };

            this.Scene.Children.Add(this._legend);
            Panel.SetZIndex(this._legend, 10000);
        }

        private void BuildGroupLegend(PersonGroup g)
        {
            // color swatches for "Most" indicators
            var taskingMost = new Rectangle { Width = 10, Height = 10, Fill = new SolidColorBrush(GetColorFromId(g.taskingMost)), Opacity = 0.75, Margin = new Thickness(0, 0, 6, 0), IsHitTestVisible = false };
            var talkingMost = new Rectangle { Width = 10, Height = 10, Fill = new SolidColorBrush(GetColorFromId(g.talkingMost)), Opacity = 0.75, Margin = new Thickness(0, 0, 6, 0), IsHitTestVisible = false };
            var watchedMost = new Rectangle { Width = 10, Height = 10, Fill = new SolidColorBrush(GetColorFromId(g.watchedMost)), Opacity = 0.75, Margin = new Thickness(0, 0, 6, 0), IsHitTestVisible = false };
            var leadVA = new Rectangle { Width = 10, Height = 10, Fill = new SolidColorBrush(GetColorFromId(g.leadVA)), Opacity = 0.75, Margin = new Thickness(0, 0, 6, 0), IsHitTestVisible = false };

            // dynamic index texts (right side)
            var _legendDominance = new TextBlock { Foreground = Brushes.White, FontSize = 12, Text = $"{Math.Round(g.dominance_score, 2)} ← Dominance score", IsHitTestVisible = false };
            var _legendJa = new TextBlock { Foreground = Brushes.White, FontSize = 12, Text = $"{Math.Round(g.ja_score, 2)} ← Joint Attention score", IsHitTestVisible = false };
            var _legendCpm = new TextBlock { Foreground = Brushes.White, FontSize = 12, Text = $"{Math.Round(g.cpm_score, 2)} ← Communication score", IsHitTestVisible = false };
            var _legendSpatial = new TextBlock { Foreground = Brushes.White, FontSize = 12, Text = $"{Math.Round(g.spatial_score, 2)} ← Spatial score", IsHitTestVisible = false };
            var _legendEngagement = new TextBlock { Foreground = Brushes.White, FontSize = 12, Text = $"{Math.Round(g.engagement_score, 2)} ← Engagement score", IsHitTestVisible = false };
            var _legendCollabScore = new TextBlock { Foreground = Brushes.White, FontSize = 12, Text = $"{Math.Round(g.collaboration_score, 2)} ← Collaboration score", IsHitTestVisible = false };
            var _legendCollabDimensionScore = new TextBlock { Foreground = Brushes.White, FontSize = 12, Text = $"{Math.Round(g.dimension2_score, 2)} ← Collaboration Dimension score", IsHitTestVisible = false };

            // helper for the "Most" rows
            StackPanel Row(UIElement icon, string text) => new()
            {
                Orientation = Orientation.Horizontal,
                Children = { icon, new TextBlock { Text = text, Foreground = Brushes.White, FontSize = 12 } },
                Margin = new Thickness(0, 2, 0, 2),
                IsHitTestVisible = false
            };

            // LEFT: the "Most" indicators
            var mostStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 0, 8, 0), // space before divider
                IsHitTestVisible = false
            };
            mostStack.Children.Add(Row(taskingMost, "← Tasking Most"));
            mostStack.Children.Add(Row(talkingMost, "← Talking Most"));
            mostStack.Children.Add(Row(watchedMost, "← Watched Most"));
            mostStack.Children.Add(Row(leadVA, "← Lead Visual Attention"));

            // CENTER: vertical divider (to the right of MOST stack)
            var vDivider = new Border
            {
                Width = 1,
                Background = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                Opacity = 0.4,
                Margin = new Thickness(0, 0, 8, 6), // space after divider
                VerticalAlignment = VerticalAlignment.Stretch,
                IsHitTestVisible = false
            };

            // RIGHT: the indexes stack (dynamic values)
            var indexStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                IsHitTestVisible = false
            };
            indexStack.Children.Add(_legendDominance);
            indexStack.Children.Add(_legendJa);
            indexStack.Children.Add(_legendCpm);
            indexStack.Children.Add(_legendSpatial);
            indexStack.Children.Add(_legendEngagement);
            indexStack.Children.Add(_legendCollabScore);
            indexStack.Children.Add(_legendCollabDimensionScore);

            // Layout container: 3 columns (MOST | divider | INDEXES)
            var grid = new Grid { IsHitTestVisible = false };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(mostStack, 0);
            Grid.SetColumn(vDivider, 1);
            Grid.SetColumn(indexStack, 2);

            grid.Children.Add(mostStack);
            grid.Children.Add(vDivider);
            grid.Children.Add(indexStack);

            // Replace previous legend if needed
            if (this._groupLegend != null)
            {
                this.Scene.Children.Remove(this._groupLegend);
                this._groupLegend = null;
            }

            this._groupLegend = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(160, 20, 20, 20)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8),
                Child = grid,
                IsHitTestVisible = false
            };

            this.Scene.Children.Add(this._groupLegend);
            Panel.SetZIndex(this._groupLegend, 10000);
        }


        /*private void BuildGroupLegend(PersonGroup g)
        {
            var taskingMost = new Rectangle { Width = 10, Height = 10, Fill = new SolidColorBrush(GetColorFromId(g.taskingMost)), Opacity = 0.75, Margin = new Thickness(0, 0, 6, 0), IsHitTestVisible = false };
            var talkingMost = new Rectangle { Width = 10, Height = 10, Fill = new SolidColorBrush(GetColorFromId(g.talkingMost)), Opacity = 0.75, Margin = new Thickness(0, 0, 6, 0), IsHitTestVisible = false };
            var watchedMost = new Rectangle { Width = 10, Height = 10, Fill = new SolidColorBrush(GetColorFromId(g.watchedMost)), Opacity = 0.75, Margin = new Thickness(0, 0, 6, 0), IsHitTestVisible = false };
            var leadVA = new Rectangle { Width = 10, Height = 10, Fill = new SolidColorBrush(GetColorFromId(g.leadVA)), Opacity = 0.75, Margin = new Thickness(0, 0, 6, 0), IsHitTestVisible = false };

            // Divider
            var divider = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                Margin = new Thickness(0, 8, 0, 6),
                Opacity = 0.4,
                IsHitTestVisible = false
            };

            // --- Dynamic text rows (placeholders, updated every frame) ---
            var _legendDominance = new TextBlock { Foreground = Brushes.White, FontSize = 12, Text = $"{Math.Round(g.dominance_score, 2)} ← Dominance score", IsHitTestVisible = false };
            var _legendJa = new TextBlock { Foreground = Brushes.White, FontSize = 12, Text = $"{Math.Round(g.ja_score, 2)} ← Joint Attention score", IsHitTestVisible = false };
            var _legendCpm = new TextBlock { Foreground = Brushes.White, FontSize = 12, Text = $"{Math.Round(g.cpm_score, 2)} ← Communication score", IsHitTestVisible = false };
            var _legendSpatial = new TextBlock { Foreground = Brushes.White, FontSize = 12, Text = $"{Math.Round(g.spatial_score, 2)} ← Spatial score", IsHitTestVisible = false };
            var _legendEngagement = new TextBlock { Foreground = Brushes.White, FontSize = 12, Text = $"{Math.Round(g.engagement_score, 2)} ← Engagement score", IsHitTestVisible = false };
            var _legendCollabScore = new TextBlock { Foreground = Brushes.White, FontSize = 12, Text = $"{Math.Round(g.collaboration_score, 2)} ← Collaboration score", IsHitTestVisible = false };
            var _legendCollabDimensionScore = new TextBlock { Foreground = Brushes.White, FontSize = 12, Text = $"{Math.Round(g.dimension2_score, 2)} ← Collaboration Dimension score", IsHitTestVisible = false };

            StackPanel Row(UIElement icon, string text) => new()
            {
                Orientation = Orientation.Horizontal,
                Children = { icon, new TextBlock { Text = text, Foreground = Brushes.White, FontSize = 12 } },
                Margin = new Thickness(0, 2, 0, 2),
                IsHitTestVisible = false
            };

            var sp = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Children =
                {
                    _legendDominance,
                    _legendJa,
                    _legendCpm,
                    _legendSpatial,
                    _legendEngagement,
                    _legendCollabScore,
                    _legendCollabDimensionScore,
                    divider,
                    Row(taskingMost, "← Tasking Most"),
                    Row(talkingMost, "← Talking Most"),
                    Row(watchedMost, "← Watched Most"),
                    Row(leadVA,      "← Lead Visual Attention"),
                },
                IsHitTestVisible = false
            };

            if (this._groupLegend != null)
            {
                this.Scene.Children.Remove(this._groupLegend);
                this._groupLegend = null;
            }

            this._groupLegend = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(160, 20, 20, 20)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8),
                Child = sp,
                IsHitTestVisible = false
            };

            this.Scene.Children.Add(this._groupLegend);
            Panel.SetZIndex(this._groupLegend, 10000);
        }
*/
        private Color GetColorFromId(double id)
        {
            return id switch
            {
                1 => Color.FromRgb(255, 255, 0),
                2 => Color.FromRgb(0, 204, 0),
                3 => Color.FromRgb(102, 0, 204),
                _ => Color.FromRgb(255, 255, 255),
            };
        }

        private void UpdateGeneralLegendPosition()
        {
            if (/*this._legend == null || */this.Scene.ActualWidth <= 0 || this.Scene.ActualHeight <= 0) return;

            // NB: la logique de positionnement est conservée telle quelle
            this._legend.LayoutTransform = new ScaleTransform(1.0 / 1.0, 1.0 / 1.0);

            this._legend.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double wScreen = this._legend.DesiredSize.Width;
            double margin = 10.0;

            Point topRightScreen = new Point(this.ActualWidth - margin, margin);

            double leftCanvas = (topRightScreen.X - wScreen);
            double topCanvas = (topRightScreen.Y);

            Canvas.SetRight(this._legend, leftCanvas);
            Canvas.SetTop(this._legend, topCanvas);
        }

        private void UpdateGroupLegendPosition()
        {
            if (this.Scene.ActualWidth <= 0 || this.Scene.ActualHeight <= 0 || this._groupLegend == null) return;

            this._groupLegend.LayoutTransform = new ScaleTransform(1.0, 1.0);

            this._groupLegend.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double wScreen = this._groupLegend.DesiredSize.Width;
            double margin = 10.0;

            Point topRightScreen = new Point(this.ActualWidth - margin, margin);

            double leftCanvas = (topRightScreen.X - wScreen);
            double topCanvas = (topRightScreen.Y);

            Canvas.SetRight(this._groupLegend, leftCanvas);
            Canvas.SetBottom(this._groupLegend, topCanvas);
        }

        // ===== Layout helpers =====

        // 3 nœuds : positions déterministes (1↑, 2↙, 3↘) à partir de distances d12/d13/d23 (déjà en mètres d'affichage)
        private Dictionary<string, Point> ComputeTriangleFromProximityMeters(
            List<PersonNode> nodes, List<PersonEdge> edges,
            double d12, double d13, double d23)
        {
            if (nodes.Count != 3) throw new InvalidOperationException("Attendu exactement 3 nœuds.");

            PersonNode n1 = nodes.FirstOrDefault(n => n.Id == "1") ?? nodes[0];
            PersonNode n2 = nodes.FirstOrDefault(n => n.Id == "2") ?? nodes[1];
            PersonNode n3 = nodes.FirstOrDefault(n => n.Id == "3") ?? nodes[2];

            EnforceTriangleInequality(ref d12, ref d13, ref d23);

            // Base horizontale (2-3)
            const double tiny = 1e-6;
            if (d23 < tiny) d23 = tiny;
            double half = d23 * 0.5;
            var p2 = new Point(-half, 0);
            var p3 = new Point(+half, 0);

            // Classique : coord de 1 depuis les 3 côtés (y négatif = "au-dessus" dans WPF)
            double x1 = (d12 * d12 - d13 * d13) / (2.0 * d23);
            double under = Math.Max(d12 * d12 - x1 * x1, 0.0);
            double y1 = -Math.Sqrt(under);

            // Altitude minimale plus ferme pour éviter le quasi-alignement
            double pxPerM = 1.0 / Math.Max(this.VO?.MetersPerPixel ?? 0.01, 1e-6);
            double minAltM = Math.Max(12.0 / pxPerM, 0.18 * d23); // ≥ 12 px et ≈ 18% de la base
            if (Math.Abs(y1) < minAltM) y1 = -minAltM;

            var p1 = new Point(x1, y1);

            // Centroïde au centre de la vue (en mètres)
            double cxm = (this.Scene.ActualWidth * 0.5) / pxPerM;
            double cym = (this.Scene.ActualHeight * 0.5) / pxPerM;

            var g = new Point((p1.X + p2.X + p3.X) / 3.0, (p1.Y + p2.Y + p3.Y) / 3.0);
            var shift = new Vector(cxm - g.X, cym - g.Y);

            return new Dictionary<string, Point>(StringComparer.Ordinal)
            {
                [n1.Id] = new Point(p1.X + shift.X, p1.Y + shift.Y),
                [n2.Id] = new Point(p2.X + shift.X, p2.Y + shift.Y),
                [n3.Id] = new Point(p3.X + shift.X, p3.Y + shift.Y),
            };
        }


        private double MapProximityMetersToDisplayDistance(double proxMeters)
        {
            double pxPerM = 1.0 / Math.Max(this.VO?.MetersPerPixel ?? 0.01, 1e-6);
            double dMin = Math.Max(this.VO?.DisplayMinDistanceMeters ?? 0.3, 8.0 / pxPerM);
            double k = this.VO?.DistanceGainK ?? 2.5;
            double d = Math.Max(0.0, proxMeters) * k;
            d = Math.Max(d, dMin);
            if (this.VO != null) d = this.VO.CompressDistanceMeters(d);
            double dMaxFit = 0.45 * Math.Min(this.Scene.ActualWidth, this.Scene.ActualHeight) / pxPerM;
            return Math.Min(d, dMaxFit);
        }

        private static void EnforceTriangleInequality(ref double d12, ref double d13, ref double d23)
        {
            const double eps = 1e-3;
            for (int it = 0; it < 3; it++)
            {
                if (d12 >= d13 + d23) d12 = Math.Max(d13 + d23 - eps, eps);
                if (d13 >= d12 + d23) d13 = Math.Max(d12 + d23 - eps, eps);
                if (d23 >= d12 + d13) d23 = Math.Max(d12 + d13 - eps, eps);
            }
        }

        // Proximity (mètres) entre deux IDs, en fusionnant les PersonEdge correspondantes (ordre indifférent)
        private static double GetProximityMeters(string a, string b, List<PersonEdge> edges, double defMeters = 1.0)
        {
            if (string.CompareOrdinal(a, b) == 0) return 0.0;
            var u = string.CompareOrdinal(a, b) <= 0 ? a : b;
            var v = string.CompareOrdinal(a, b) <= 0 ? b : a;

            var vals = edges
                .Select(e =>
                {
                    var ids = new[] { e.FromId_1, e.ToId_1, e.FromId_2, e.ToId_2 }
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(s => s, StringComparer.Ordinal)
                        .ToArray();
                    bool match = ids.Length >= 2 && ids[0] == u && ids[1] == v;
                    return match ? (double?)Math.Max(0.0, e.Proximity) : null;
                })
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .ToArray();

            return vals.Length == 0 ? defMeters : vals.Average();
        }


        private Dictionary<string, Point> ComputeTargetsMeters(List<PersonNode> nodes, List<PersonEdge> edges)
        {
            var t = new Dictionary<string, Point>(StringComparer.Ordinal);
            var centerPx = new Point(this.Scene.ActualWidth / 2, this.Scene.ActualHeight / 2);

            double pxPerM = 1.0 / Math.Max(this.VO.MetersPerPixel, 1e-6);
            double radiusPx = Math.Min(this.Scene.ActualWidth, this.Scene.ActualHeight) * 0.35;
            double radiusM = radiusPx / pxPerM;
            double cx = centerPx.X / pxPerM;
            double cy = centerPx.Y / pxPerM;

            for (int i = 0; i < nodes.Count; i++)
            {
                var id = nodes[i].Id;
                if (!this.posM.TryGetValue(id, out var p))
                {
                    double ang = 2 * Math.PI * i / Math.Max(1, nodes.Count);
                    p = new Point(cx + radiusM * Math.Cos(ang), cy + radiusM * Math.Sin(ang));
                }
                t[id] = p;
            }

            const double k = 0.15;
            foreach (var e in edges)
            {
                var (a, b) = GetPair(e);
                if (!t.ContainsKey(a) || !t.ContainsKey(b)) continue;

                var pa = t[a];
                var pb = t[b];
                var v = new Vector(pb.X - pa.X, pb.Y - pa.Y);
                var d = v.Length;
                if (d < 1e-3) d = 1e-3;

                double proxMeters = e.Proximity;
                double dTarget = MapProximityMetersToDisplayDistance(proxMeters);

                pxPerM = 1.0 / Math.Max(this.VO.MetersPerPixel, 1e-6);
                double maxDisplayM = 0.45 * Math.Min(this.Scene.ActualWidth, this.Scene.ActualHeight) / pxPerM;
                dTarget = Math.Min(dTarget, maxDisplayM);

                var dir = v / d;
                var corr = k * (d - dTarget);

                t[a] = new Point(pa.X + corr * dir.X, pa.Y + corr * dir.Y);
                t[b] = new Point(pb.X - corr * dir.X, pb.Y - corr * dir.Y);
            }

            // répulsion douce sous la distance mini
            foreach (var i in nodes)
            {
                foreach (var j in nodes)
                {
                    if (ReferenceEquals(i, j)) continue;

                    var pi = t[i.Id];
                    var pj = t[j.Id];
                    var dv = new Vector(pi.X - pj.X, pi.Y - pj.Y);
                    var d = dv.Length;
                    if (d < 1e-6) continue;

                    if (d < this.VO.DisplayMinDistanceMeters)
                    {
                        var push = (this.VO.DisplayMinDistanceMeters - d) / this.VO.DisplayMinDistanceMeters;
                        dv /= d;
                        t[i.Id] = new Point(pi.X + dv.X * push * 0.1, pi.Y + dv.Y * push * 0.1);
                    }
                }
            }

            return t;
        }

        private static (int, int, int) MakeTriKey(double d12, double d13, double d23)
        {
            int k12 = (int)Math.Round(d12 * 1000.0);
            int k13 = (int)Math.Round(d13 * 1000.0);
            int k23 = (int)Math.Round(d23 * 1000.0);
            return (k12, k13, k23);
        }

        private static bool ShouldSnapToTargets(
            (int k12, int k13, int k23) curKey,
            (int k12, int k13, int k23)? prevKey,
            (double d12, double d13, double d23) curMeters,
            (double d12, double d13, double d23)? prevMeters,
            bool cacheHit)
        {
            if (cacheHit) return true;
            if (!prevKey.HasValue || !prevMeters.HasValue) return true;
            var p = prevMeters.Value;
            double sumAbs = Math.Abs(curMeters.d12 - p.d12) + Math.Abs(curMeters.d13 - p.d13) + Math.Abs(curMeters.d23 - p.d23);
            if (sumAbs < TinySignatureEpsMeters) return true;
            if (sumAbs > BigJumpSumMeters) return true;
            return false;
        }

        private static Point RotateAround(Point p, Point center, double radians)
        {
            double c = Math.Cos(radians), s = Math.Sin(radians);
            double x = p.X - center.X, y = p.Y - center.Y;
            return new Point(center.X + c * x - s * y, center.Y + s * x + c * y);
        }


        // 3 nœuds : positions déterministes (1↑, 2↙, 3↘) avec inclinaison limitée pour libérer Y à 2 et 3.
        // Entrée : distances d12/d13/d23 (déjà en mètres d’affichage).
        private Dictionary<string, Point> ComputeTriangleWithTilt(
            List<PersonNode> nodes, (double d12, double d13, double d23) d)
        {
            if (nodes.Count != 3) throw new InvalidOperationException("Attendu exactement 3 nœuds.");

            PersonNode n1 = nodes.FirstOrDefault(n => n.Id == "1") ?? nodes[0];
            PersonNode n2 = nodes.FirstOrDefault(n => n.Id == "2") ?? nodes[1];
            PersonNode n3 = nodes.FirstOrDefault(n => n.Id == "3") ?? nodes[2];

            double d12 = d.d12, d13 = d.d13, d23 = d.d23;
            EnforceTriangleInequality(ref d12, ref d13, ref d23);

            // --- Construction canonique : base horizontale (2-3) ---
            const double tiny = 1e-6;
            if (d23 < tiny) d23 = tiny;
            double half = d23 * 0.5;
            var p2 = new Point(-half, 0.0);
            var p3 = new Point(+half, 0.0);

            // Coord de 1 (y négatif = "au-dessus" en WPF)
            double x1 = (d12 * d12 - d13 * d13) / (2.0 * d23);
            double under = Math.Max(d12 * d12 - x1 * x1, 0.0);
            double y1 = -Math.Sqrt(under);

            // Altitude mini plus ferme (évite quasi-alignement)
            double pxPerM = 1.0 / Math.Max(this.VO?.MetersPerPixel ?? 0.01, 1e-6);
            double minAltM = Math.Max(12.0 / pxPerM, 0.18 * d23); // ≥ 12px & ~18% base
            if (Math.Abs(y1) < minAltM) y1 = -minAltM;

            var p1 = new Point(x1, y1);

            // --- Inclinaison contrôlée autour du centroïde (libère Y pour 2 & 3) ---
            var g0 = new Point((p1.X + p2.X + p3.X) / 3.0, (p1.Y + p2.Y + p3.Y) / 3.0);

            // Asymétrie : d13 > d12 → phi > 0 (p2 monte, p3 descend)
            double asym = (d13 - d12) / Math.Max(d12 + d13, 1e-6);
            double phi0 = (TiltMaxDeg * Math.PI / 180.0) * TiltGain * asym;

            // Bande verticale autorisée pour 2/3
            double bandM = Math.Max(BandMinPx / pxPerM, BandRatioOfBase * d23);

            // Test et réduction éventuelle de phi pour rester dans la bande
            Point p2r = RotateAround(p2, g0, phi0);
            Point p3r = RotateAround(p3, g0, phi0);
            double devMax = Math.Max(Math.Abs(p2r.Y - g0.Y), Math.Abs(p3r.Y - g0.Y));
            if (devMax > bandM && devMax > 1e-9)
            {
                double scale = bandM / devMax;
                phi0 *= scale;
                p2r = RotateAround(p2, g0, phi0);
                p3r = RotateAround(p3, g0, phi0);
            }
            Point p1r = RotateAround(p1, g0, phi0);

            // --- Recentre le centroïde au milieu de la vue ---
            double cxm = (this.Scene.ActualWidth * 0.5) / pxPerM;
            double cym = (this.Scene.ActualHeight * 0.5) / pxPerM;

            var g = new Point((p1r.X + p2r.X + p3r.X) / 3.0, (p1r.Y + p2r.Y + p3r.Y) / 3.0);
            var shift = new Vector(cxm - g.X, cym - g.Y);

            return new Dictionary<string, Point>(StringComparer.Ordinal)
            {
                [n1.Id] = new Point(p1r.X + shift.X, p1r.Y + shift.Y), // 1 reste en haut
                [n2.Id] = new Point(p2r.X + shift.X, p2r.Y + shift.Y), // 2 bas-gauche (peut monter/descendre)
                [n3.Id] = new Point(p3r.X + shift.X, p3r.Y + shift.Y), // 3 bas-droite (peut monter/descendre)
            };
        }

        // ===== Création / MAJ des visuels =====
        private void EnsureNodeVisuals(List<PersonNode> nodes)
        {
            var ids = nodes.Select(n => n.Id).ToHashSet(StringComparer.Ordinal);

            foreach (var id in this.nodeShapes.Keys.Except(ids).ToList())
            {
                if (this.nodeSurfaces.TryGetValue(id, out var surf))
                {
                    this.Scene.Children.Remove(surf);
                    this.nodeSurfaces.Remove(id);
                }
                this.Scene.Children.Remove(this.nodeShapes[id]);
                this.Scene.Children.Remove(this.nodeLabels[id]);
                this.nodeShapes.Remove(id);
                this.nodeLabels.Remove(id);
                this.posM.Remove(id);
                this.velM.Remove(id);
            }

            foreach (var n in nodes)
            {
                if (!this.nodeShapes.ContainsKey(n.Id))
                {
                    Brush color = Brushes.White;
                    switch (int.Parse(n.Id))
                    {
                        case 1: color = Brushes.Yellow; break;
                        case 2: color = Brushes.Green; break;
                        case 3: color = Brushes.Purple; break;
                    }

                    var surface = new Ellipse { IsHitTestVisible = false, Opacity = SurfaceOpacity };
                    this.Scene.Children.Add(surface);
                    this.nodeSurfaces[n.Id] = surface;

                    var ell = new Ellipse
                    {
                        Fill = color,
                        Stroke = Brushes.White,
                        StrokeThickness = 1.5,
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        { BlurRadius = 6, ShadowDepth = 0, Opacity = 0.35, Color = Colors.Black },
                        IsHitTestVisible = false
                    };
                    this.Scene.Children.Add(ell);
                    this.nodeShapes[n.Id] = ell;

                    var tb = new TextBlock
                    {
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 15,
                        Text = n.Id,
                        IsHitTestVisible = false
                    };
                    this.Scene.Children.Add(tb);
                    this.nodeLabels[n.Id] = tb;
                }
            }
        }

        private void EnsureEdgeVisuals(List<PersonEdge> edges)
        {
            var keys = edges
                .Select(e => MakeKey(GetPair(e).a, GetPair(e).b))
                .ToHashSet(StringComparer.Ordinal);

            // Supprimer les obsolètes
            foreach (var k in this.edgeGaze.Keys.Except(keys).ToList()) { this.Scene.Children.Remove(this.edgeGaze[k]); this.edgeGaze.Remove(k); }
            foreach (var k in this.edgeSpeech.Keys.Except(keys).ToList()) { this.Scene.Children.Remove(this.edgeSpeech[k]); this.edgeSpeech.Remove(k); }
            foreach (var k in this.edgeGazeArrow12.Keys.Except(keys).ToList()) { this.Scene.Children.Remove(this.edgeGazeArrow12[k]); this.edgeGazeArrow12.Remove(k); }
            foreach (var k in this.edgeGazeArrow21.Keys.Except(keys).ToList()) { this.Scene.Children.Remove(this.edgeGazeArrow21[k]); this.edgeGazeArrow21.Remove(k); }
            foreach (var k in this.edgeSyncCurve.Keys.Except(keys).ToList()) { this.Scene.Children.Remove(this.edgeSyncCurve[k]); this.edgeSyncCurve.Remove(k); }
            foreach (var k in this.edgeSynchCapsules.Keys.Except(keys).ToList()) { this.Scene.Children.Remove(this.edgeSynchCapsules[k]); this.edgeSynchCapsules.Remove(k); }

            // Créer les manquantes (Z-order: 0 Gaze, 1 Gaze A->B, 2 Gaze B->A, 3 Speech/Sync/Capsule, puis nodes)
            foreach (var e in edges)
            {
                var key = MakeKey(GetPair(e).a, GetPair(e).b);

                if (!this.edgeGaze.ContainsKey(key))
                {
                    var pGaze = new Path { Stroke = new SolidColorBrush(ColGreen), StrokeThickness = 2, Opacity = 0.9, IsHitTestVisible = false };
                    this.Scene.Children.Insert(0, pGaze);
                    this.edgeGaze[key] = pGaze;
                }
                if (!this.edgeGazeArrow12.ContainsKey(key))
                {
                    var a12 = new Path { Fill = new SolidColorBrush(ColGreen), Opacity = 0.95, Visibility = Visibility.Collapsed, IsHitTestVisible = false };
                    this.Scene.Children.Insert(1, a12);
                    this.edgeGazeArrow12[key] = a12;
                }
                if (!this.edgeGazeArrow21.ContainsKey(key))
                {
                    var a21 = new Path { Fill = new SolidColorBrush(ColGreen), Opacity = 0.95, Visibility = Visibility.Collapsed, IsHitTestVisible = false };
                    this.Scene.Children.Insert(2, a21);
                    this.edgeGazeArrow21[key] = a21;
                }
                if (!this.edgeSpeech.ContainsKey(key))
                {
                    var pSpeech = new Path { Stroke = Brushes.Gray, StrokeThickness = 2, Opacity = 0.95, IsHitTestVisible = false };
                    this.Scene.Children.Insert(3, pSpeech);
                    this.edgeSpeech[key] = pSpeech;
                }
                if (!this.edgeSyncCurve.ContainsKey(key))
                {
                    var p = new Path
                    {
                        Stroke = new SolidColorBrush(ColSyncCurve),
                        StrokeThickness = SyncThicknessMin,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round,
                        Opacity = SyncOpacityMin,
                        IsHitTestVisible = false
                    };
                    this.Scene.Children.Insert(3, p);
                    this.edgeSyncCurve[key] = p;
                }
                if (!this.edgeSynchCapsules.ContainsKey(key))
                {
                    var psynch = new Path { Stroke = Brushes.Gray, StrokeThickness = 10, Opacity = 0.75, IsHitTestVisible = false };
                    this.Scene.Children.Insert(3, psynch);
                    this.edgeSynchCapsules[key] = psynch;
                }
            }
        }
    }
}
