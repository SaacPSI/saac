using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SAAC.CollaborationIndices
{
    /// <summary>Une sous-tache : nom, bornes temporelles, participants concernes (null = tous).</summary>
    public class SubTask
    {
        public string Name;
        public DateTime Start;
        public DateTime End;
        public int[] Participants;

        public double DurationSeconds { get { return (End - Start).TotalSeconds; } }
    }

    /// <summary>Une verbalisation : qui, de quand a quand.</summary>
    public class Verbalization
    {
        public int Id;
        public DateTime Start;
        public DateTime End;
    }

    /// <summary>
    /// Definition des sous-taches d'une session et calcul des indicateurs verbaux
    /// (parole, silence, cross-talk, egalite de parole) pour chacune d'elles.
    /// </summary>
    public class SubTaskVerbalMetrics
    {
        private readonly string session;
        private readonly string condition;
        private readonly int participantCount;
        private readonly TimeSpan offset;
        private readonly List<SubTask> tasks = new List<SubTask>();

        /// <param name="offset">Decalage horloge terrain -> horloge du store (ancien CheckTimeSpanForSession).</param>
        public SubTaskVerbalMetrics(string session, int participantCount, TimeSpan offset)
        {
            this.session = session;
            this.participantCount = participantCount;
            this.offset = offset;
        }

        public List<SubTask> Tasks { get { return tasks; } }

        // -----------------------------------------------------------------
        // 1. Definition des sous-taches
        // -----------------------------------------------------------------

        /// <summary>Ajoute une sous-tache a partir de timestamps Unix en millisecondes.</summary>
        public void AddTask(string name, long startUnixMs, long endUnixMs, int[] participants = null)
        {
            tasks.Add(new SubTask
            {
                Name = name,
                Start = Unix(startUnixMs) - offset,
                End = Unix(endUnixMs) - offset,
                Participants = participants
            });
        }

        public static DateTime Unix(long unixMilliseconds)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds).UtcDateTime;
        }

        /// <summary>
        /// Sous-tache active a un instant donne, ou null. En cas d'imbrication
        /// (Tabaluga16th dans Tabaluga), la plus courte est retournee.
        /// </summary>
        public SubTask TaskAt(DateTime time)
        {
            return tasks.Where(t => time >= t.Start && time < t.End)
                        .OrderBy(t => t.End - t.Start)
                        .FirstOrDefault();
        }

        /// <summary>
        /// Bornes de chaque sous-tache, par session (timestamps Unix en millisecondes).
        /// Tabaluga16th et Tabaluga18th sont des sous-parties de Tabaluga (fenetres imbriquees).
        /// Pour ajouter une session, dupliquer un bloc case et remplacer les valeurs.
        /// </summary>
        public static SubTaskVerbalMetrics Setup(string session, int participantCount = 2, TimeSpan offset = default(TimeSpan))
        {
            var s = new SubTaskVerbalMetrics(session, participantCount, offset);

            switch (session)
            {
                case "1":
                    // s.AddTask("Intro", 1786377347240, 1786377445029);
                    s.AddTask("FindIt_1", 1786377487327, 1786377613187);
                    s.AddTask("Quiz1", 1786377674665, 1786377806752);
                    // s.AddTask("Tabaluga", 1786378006321, 1786378331953);
                    s.AddTask("Tabaluga16th", 1786378061873, 1786378194437);
                    s.AddTask("Tabaluga18th", 1786378260129, 1786378331953);
                    s.AddTask("Quiz2", 1786378555044, 1786378664174);
                    s.AddTask("FindIt_2", 1786378751517, 1786378856074);
                    break;

                case "2":
                    // s.AddTask("Intro", 1786453151922, 1786453193813);
                    s.AddTask("FindIt_1", 1786453218616, 1786453316695);
                    s.AddTask("Quiz1", 1786453376815, 1786453555397);
                    // s.AddTask("Tabaluga", 1786454307281, 1786454498898);
                    s.AddTask("Tabaluga16th", 1786454337414, 1786454348098);
                    s.AddTask("Tabaluga18th", 1786454365382, 1786454498898);
                    s.AddTask("Quiz2", 1786454773725, 1786454958570);
                    s.AddTask("FindIt_2", 1786454983625, 1786455092064);
                    break;

                case "3":
                    // s.AddTask("Intro", 1786463642812, 1786463742587);
                    s.AddTask("FindIt_1", 1786463788663, 1786463882942);
                    s.AddTask("Quiz1", 1786463928644, 1786464305474);
                    // s.AddTask("Tabaluga", 1786465071477, 1786465370626);
                    s.AddTask("Tabaluga16th", 1786465109301, 1786465149327);
                    s.AddTask("Tabaluga18th", 1786465187404, 1786465370626);
                    s.AddTask("Quiz2", 1786465592144, 1786465782660);
                    s.AddTask("FindIt_2", 1786465801758, 1786465924781);
                    break;

                case "4":
                    // s.AddTask("Intro", 1786529844785, 1786529873515);
                    s.AddTask("FindIt_1", 1786529916993, 1786530042934);
                    s.AddTask("Quiz1", 1786530106843, 1786530289090);
                    // s.AddTask("Tabaluga", 1786530477808, 1786530860838);
                    s.AddTask("Tabaluga16th", 1786530666990, 1786530769023);
                    s.AddTask("Tabaluga18th", 1786530775814, 1786530860838);
                    s.AddTask("Quiz2", 1786531144087, 1786531357179);
                    s.AddTask("FindIt_2", 1786531377015, 1786531459323);
                    break;

                case "5":
                    // s.AddTask("Intro", 1786625686270, 1786625706304);
                    s.AddTask("FindIt_1", 1786625745556, 1786625869929);
                    s.AddTask("Quiz1", 1786625913130, 1786626062106);
                    // s.AddTask("Tabaluga", 1786626242000, 1786626562393);
                    s.AddTask("Tabaluga16th", 1786626344799, 1786626474843);
                    s.AddTask("Tabaluga18th", 1786626492893, 1786626562393);
                    s.AddTask("Quiz2", 1786626846446, 1786626949465);
                    s.AddTask("FindIt_2", 1786626986855, 1786627080787);
                    break;

                case "6":
                    // s.AddTask("Intro", 1786636479147, 1786636493427);
                    s.AddTask("FindIt_1", 1786636504003, 1786636621739);
                    s.AddTask("Quiz1", 1786636672398, 1786636828761);
                    // s.AddTask("Tabaluga",                 ?, 1786637340462);  // debut manquant dans les donnees brutes
                    s.AddTask("Tabaluga16th", 1786637084590, 1786637195495);
                    s.AddTask("Tabaluga18th", 1786637202700, 1786637340462);
                    s.AddTask("Quiz2", 1786637619324, 1786637828456);
                    s.AddTask("FindIt_2", 1786637867144, 1786638000961);
                    break;

                case "7":
                    // s.AddTask("Intro", 1786707304000, 1786707337048);
                    s.AddTask("FindIt_1", 1786707392436, 1786707516225);
                    s.AddTask("Quiz1", 1786707567233, 1786707809870);
                    // s.AddTask("Tabaluga", 1786707983555, 1786708405714);
                    s.AddTask("Tabaluga16th", 1786708098782, 1786708262492);
                    s.AddTask("Tabaluga18th", 1786708273892, 1786708405714);
                    s.AddTask("Quiz2", 1786708674766, 1786708847782);
                    s.AddTask("FindIt_2", 1786708874936, 1786709000727);
                    break;

                case "8":
                    // s.AddTask("Intro", 1786712229498, 1786712253737);
                    s.AddTask("FindIt_1", 1786712258248, 1786712335000);
                    s.AddTask("Quiz1", 1786712847037, 1786713102856);
                    // s.AddTask("Tabaluga", 1786713278508, 1786713506856);
                    s.AddTask("Tabaluga16th", 1786713325656, 1786713421782);
                    s.AddTask("Tabaluga18th", 1786713455202, 1786713506856);
                    s.AddTask("Quiz2", 1786713815174, 1786713928733);
                    s.AddTask("FindIt_2", 1786713940000, 1786714033852);
                    break;

                case "9":
                    // s.AddTask("Intro", 1786793293401, 1786793348419);
                    s.AddTask("FindIt_1", 1786793373608, 1786793498951);
                    s.AddTask("Quiz1", 1786793559505, 1786793864907);
                    // s.AddTask("Tabaluga", 1786794057725, 1786794385829);
                    s.AddTask("Tabaluga16th", 1786794146241, 1786794291372);
                    s.AddTask("Tabaluga18th", 1786794294843, 1786794385829);
                    s.AddTask("Quiz2", 1786794645900, 1786794791383);
                    s.AddTask("FindIt_2", 1786794808005, 1786794940420);
                    break;

                case "10":
                    // s.AddTask("Intro", 1786876280103, 1786876375969);
                    s.AddTask("FindIt_1", 1786876414072, 1786876540337);
                    s.AddTask("Quiz1", 1786876610315, 1786876864929);
                    // s.AddTask("Tabaluga", 1786877051080, 1786877693983);
                    s.AddTask("Tabaluga16th", 1786877314991, 1786877571237);
                    s.AddTask("Tabaluga18th", 1786877580440, 1786877693983);
                    s.AddTask("Quiz2", 1786877969055, 1786878056173);
                    s.AddTask("FindIt_2", 1786878102785, 1786878215970);
                    break;

                case "11":
                    // s.AddTask("Intro", 1787047927664, 1787048048275);
                    s.AddTask("FindIt_1", 1787048122799, 1787048182390);
                    s.AddTask("Quiz1", 1787048547850, 1787048730463);
                    // s.AddTask("Tabaluga", 1787048919262, 1787049136321);
                    s.AddTask("Tabaluga16th", 1787048962275, 1787049052154);
                    s.AddTask("Tabaluga18th", 1787049084232, 1787049136321);
                    s.AddTask("Quiz2", 1787049380760, 1787049439789);
                    s.AddTask("FindIt_2", 1787049465152, 1787049488968);
                    break;

                case "12":
                    // s.AddTask("Intro", 1787056274117, 1787056321420);
                    s.AddTask("FindIt_1", 1787056349588, 1787056526510);
                    s.AddTask("Quiz1", 1787056580306, 1787056749135);
                    // s.AddTask("Tabaluga",                 ?, 1787057298433);  // debut manquant dans les donnees brutes
                    s.AddTask("Tabaluga16th", 1787057025966, 1787057200477);
                    s.AddTask("Tabaluga18th", 1787057288543, 1787057298433);
                    s.AddTask("Quiz2", 1787057571872, 1787057699878);
                    s.AddTask("FindIt_2", 1787057716356, 1787057816832);
                    break;
                default:
                    throw new ArgumentException("Session inconnue : " + session);
            }

            return s;
        }

        // -----------------------------------------------------------------
        // 2. Calcul des indicateurs
        // -----------------------------------------------------------------

        public class TaskResult
        {
            public string Task;
            public double DurationSeconds;
            public int[] Participants;
            public double[] Speak;          // temps de parole par participant
            public double[] Silence;        // duree tache - parole du participant
            public double GroupSpeak;       // au moins une personne parle
            public double GroupSilence;     // personne ne parle
            public double CrossTalk;        // au moins deux personnes parlent
            public int CrossTalkCount;
            public double EqualityIndex;    // 1 - Gini
        }

        /// <summary>Calcule les indicateurs d'une sous-tache.</summary>
        public TaskResult Compute(SubTask task, List<Verbalization> verbalizations)
        {
            int[] ids = task.Participants ?? Enumerable.Range(0, participantCount).ToArray();

            // Verbalisations rognees sur la fenetre de la sous-tache, triees par debut
            var events = verbalizations
                .Where(v => ids.Contains(v.Id) && v.End > task.Start && v.Start < task.End)
                .Select(v => new Verbalization
                {
                    Id = v.Id,
                    Start = v.Start < task.Start ? task.Start : v.Start,
                    End = v.End > task.End ? task.End : v.End
                })
                .OrderBy(v => v.Start)
                .ToList();

            var result = new TaskResult
            {
                Task = task.Name,
                DurationSeconds = task.DurationSeconds,
                Participants = ids,
                Speak = new double[ids.Length],
                Silence = new double[ids.Length]
            };

            // Temps de parole individuel
            for (int i = 0; i < ids.Length; i++)
            {
                result.Speak[i] = events.Where(v => v.Id == ids[i]).Sum(v => (v.End - v.Start).TotalSeconds);
                result.Silence[i] = task.DurationSeconds - result.Speak[i];
            }

            // Parole de groupe (union) et cross-talk (intersections), en un seul passage
            DateTime blockStart = DateTime.MinValue, blockEnd = DateTime.MinValue;
            foreach (var v in events)
            {
                if (v.Start < blockEnd)
                {
                    // chevauchement avec ce qui precede
                    DateTime overlapEnd = v.End < blockEnd ? v.End : blockEnd;
                    result.CrossTalk += (overlapEnd - v.Start).TotalSeconds;
                    result.CrossTalkCount++;
                    if (v.End > blockEnd) blockEnd = v.End;
                }
                else
                {
                    // nouveau bloc de parole
                    if (blockEnd > blockStart) result.GroupSpeak += (blockEnd - blockStart).TotalSeconds;
                    blockStart = v.Start;
                    blockEnd = v.End;
                }
            }
            if (blockEnd > blockStart) result.GroupSpeak += (blockEnd - blockStart).TotalSeconds;

            result.GroupSilence = task.DurationSeconds - result.GroupSpeak;
            result.EqualityIndex = 1.0 - Gini(result.Speak);
            return result;
        }

        /// <summary>Calcule les indicateurs de toutes les sous-taches.</summary>
        public List<TaskResult> ComputeAll(List<Verbalization> verbalizations)
        {
            return this.tasks.Select(t => this.Compute(t, verbalizations)).ToList();
        }

        /// <summary>Indice de Gini : 0 = parole parfaitement repartie, 1 = monopolisee.</summary>
        public static double Gini(double[] values)
        {
            if (values.Length < 2) return 0;
            double[] sorted = values.OrderBy(v => v).ToArray();
            double total = sorted.Sum();
            if (total <= 0) return 0;

            double cumulative = 0;
            for (int i = 0; i < sorted.Length; i++) cumulative += (i + 1) * sorted[i];

            double gini = (2.0 * cumulative) / (sorted.Length * total) - (sorted.Length + 1.0) / sorted.Length;
            return gini < 0 ? 0 : gini;
        }

        // -----------------------------------------------------------------
        // 3. Export CSV
        // -----------------------------------------------------------------

        public void WriteCsv(string path, List<TaskResult> results)
        {
            using (var w = new StreamWriter(path))
            {
                w.WriteLine("session;condition;task;duration_seconds;userid;total_speak;total_silence;ratio_speak;ratio_silence;group_speak;group_silence;crosstalk;crosstalk_count;equality_index");

                foreach (var r in results)
                {
                    for (int i = 0; i < r.Participants.Length; i++)
                    {
                        w.WriteLine(string.Join(";", new[]
                        {
                            session,
                            condition,
                            r.Task,
                            N(r.DurationSeconds),
                            r.Participants[i].ToString(CultureInfo.InvariantCulture),
                            N(r.Speak[i]),
                            N(r.Silence[i]),
                            N(r.Speak[i] / r.DurationSeconds),
                            N(r.Silence[i] / r.DurationSeconds),
                            N(r.GroupSpeak),
                            N(r.GroupSilence),
                            N(r.CrossTalk),
                            r.CrossTalkCount.ToString(CultureInfo.InvariantCulture),
                            N(r.EqualityIndex)
                        }));
                    }
                }
            }
        }

        private static string N(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value)
                ? ""
                : value.ToString("F3", CultureInfo.InvariantCulture);
        }
    }
}
