// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.Serialization;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Implements a visualization object for PlayersData.
    /// </summary>
    [VisualizationObject("PlayersData")]
    [VisualizationPanelType(VisualizationPanelType.Canvas)]
    public class PlayersDataVisualizationObject : StreamValueVisualizationObject<List<PlayersData>>, INotifyPropertyChanged
    {
        private bool showPlayersName = true;
        private bool showPlayersObjectView = true;
        private string sceneImage = "";
        private RotationAngleEnum currentRotation = RotationAngleEnum.Angle0;

        /// <inheritdoc/>
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(PlayersDataVisualizationObjectView));

        // On update
        protected override void OnPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            base.OnPropertyChanging(sender, e);

            if (e.PropertyName == nameof(this.CurrentValue))
            {
                this.RaisePropertyChanging(nameof(this.Players));
                this.RaisePropertyChanging(nameof(this.ShowPlayersName));
                this.RaisePropertyChanging(nameof(this.ShowPlayersObjectView));
            }
        }

        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.CurrentValue))
            {
                this.RaisePropertyChanged(nameof(this.Players));
                this.RaisePropertyChanged(nameof(this.ShowPlayersName));
                this.RaisePropertyChanged(nameof(this.ShowPlayersObjectView));
            }

            base.OnPropertyChanged(sender, e);
        }

        [DataMember]
        [DisplayName("Scene Image")]
        [Description("Display the selected image to the background to match your scene")]
        public string SceneImage { 
            get => sceneImage;
            set {
                sceneImage = value;
                this.RaisePropertyChanged(nameof(this.SceneImage));
            }
        }


        public List<PlayersData> Players
        {
            get 
            {
                if (this.CurrentValue.HasValue)
                {
                    return this.CurrentValue.Value.Data;
                }
                return new List<PlayersData>();
            }
        }

        [DataMember]
        [DisplayName("Show players name")]
        [Description("Show the players names right under their positions")]
        public bool ShowPlayersName
        {
            get { return this.showPlayersName; }
            set {
                this.showPlayersName = value;
                this.RaisePropertyChanged(nameof(ShowPlayersName));
            }
        }

        [DataMember]
        [DisplayName("Show players object view")]
        [Description("Show the object a player is looking at right under their positions")]
        public bool ShowPlayersObjectView
        {
            get { return this.showPlayersObjectView; }
            set
            {
                this.showPlayersObjectView = value;
                this.RaisePropertyChanged(nameof(ShowPlayersObjectView));
            }
        }

        [DataMember]
        [DisplayName("Rotation angle")]
        [Description("Select the rotation angle")]
        public RotationAngleEnum RotationAngle {
            get => currentRotation;
            set
            {
                currentRotation = value;
                this.RaisePropertyChanged(nameof(RotationAngle));
            }
        }

        public enum RotationAngleEnum
        {
            [Description("0°")]
            Angle0 = 0,

            [Description("90°")]
            Angle90 = 90,

            [Description("180°")]
            Angle180 = 180,

            [Description("270°")]
            Angle270 = 270
        }
    }
}