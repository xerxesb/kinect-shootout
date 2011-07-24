/////////////////////////////////////////////////////////////////////////
//
// This module contains code to do Kinect NUI initialization and
// processing and also to display NUI streams on screen.
//
// Copyright © Microsoft Corporation.  All rights reserved.  
// This code is licensed under the terms of the 
// Microsoft Kinect for Windows SDK (Beta) from Microsoft Research 
// License Agreement: http://research.microsoft.com/KinectSDK-ToU
//
/////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Research.Kinect.Nui;

namespace Shootout
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        Runtime _nui;

        readonly Dictionary<JointID, Brush> _jointColors = new Dictionary<JointID, Brush>
        { 
            {JointID.HipCenter, new SolidColorBrush(Color.FromRgb(169, 176, 155))},
            {JointID.Spine, new SolidColorBrush(Color.FromRgb(169, 176, 155))},
            {JointID.ShoulderCenter, new SolidColorBrush(Color.FromRgb(168, 230, 29))},
            {JointID.Head, new SolidColorBrush(Color.FromRgb(200, 0,   0))},
            {JointID.ShoulderLeft, new SolidColorBrush(Color.FromRgb(79,  84,  33))},
            {JointID.ElbowLeft, new SolidColorBrush(Color.FromRgb(84,  33,  42))},
            {JointID.WristLeft, new SolidColorBrush(Color.FromRgb(255, 126, 0))},
            {JointID.HandLeft, new SolidColorBrush(Color.FromRgb(215,  86, 0))},
            {JointID.ShoulderRight, new SolidColorBrush(Color.FromRgb(33,  79,  84))},
            {JointID.ElbowRight, new SolidColorBrush(Color.FromRgb(33,  33,  84))},
            {JointID.WristRight, new SolidColorBrush(Color.FromRgb(77,  109, 243))},
            {JointID.HandRight, new SolidColorBrush(Color.FromRgb(37,   69, 243))},
            {JointID.HipLeft, new SolidColorBrush(Color.FromRgb(77,  109, 243))},
            {JointID.KneeLeft, new SolidColorBrush(Color.FromRgb(69,  33,  84))},
            {JointID.AnkleLeft, new SolidColorBrush(Color.FromRgb(229, 170, 122))},
            {JointID.FootLeft, new SolidColorBrush(Color.FromRgb(255, 126, 0))},
            {JointID.HipRight, new SolidColorBrush(Color.FromRgb(181, 165, 213))},
            {JointID.KneeRight, new SolidColorBrush(Color.FromRgb(71, 222,  76))},
            {JointID.AnkleRight, new SolidColorBrush(Color.FromRgb(245, 228, 156))},
            {JointID.FootRight, new SolidColorBrush(Color.FromRgb(77,  109, 243))}
        };

        private List<Player> _players;

        private void HandleWindowLoaded(object sender, EventArgs e)
        {
            InitKinect();
            InitGame();
        }

        private void InitGame()
        {
            _players = new List<Player>();
        }

        private void InitKinect()
        {
            _nui = new Runtime();

            try
            {
                _nui.Initialize(RuntimeOptions.UseDepthAndPlayerIndex | RuntimeOptions.UseSkeletalTracking | RuntimeOptions.UseColor);
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show("Runtime initialization failed. Please make sure Kinect device is plugged in.");
                return;
            }

            try
            {
                _nui.VideoStream.Open(ImageStreamType.Video, 2, ImageResolution.Resolution640x480, ImageType.Color);
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show("Failed to open stream. Please make sure to specify a supported image type and resolution.");
                return;
            }

            _nui.SkeletonFrameReady += HandleSkeletonFrameReady;
            _nui.SkeletonFrameReady += HandleGameStep;
        }

        private void HandleGameStep(object sender, SkeletonFrameReadyEventArgs e)
        {
            var players = IdentifyPlayers(e.SkeletonFrame);

            foreach (var player in players)
            {
                var leftAngle = (player.LeftElbow.Position.Y - player.LeftHand.Position.Y) / (player.LeftElbow.Position.Z - player.LeftHand.Position.Z);
                var rightAngle = (player.RightElbow.Position.Y - player.RightHand.Position.Y) / (player.RightElbow.Position.Z - player.RightHand.Position.Z);

                player.LeftAngle = leftAngle;
                player.RightAngle = rightAngle;

                player.Fired = leftAngle < 0.05 || rightAngle < 0.05;
                if (player.Fired)
                {
                    player.ShootTime = DateTime.Now;
                }

                if (player.LeftHand.Position.X < player.LastLeftHandXPosition)
                {
                    player.LastLeftHandXPosition = player.LeftHand.Position.X;
                    Console.WriteLine("Player {0}, LH last XPos{1}]", player.PlayerId, player.LastLeftHandXPosition);

                    if (player.Ready)
                    {
                        player.Fired = true;
                        player.ShootTime = DateTime.Now;
                        CheckState(players);
                    }
                }

                if (player.RightHand.Position.X < player.LastRightHandXPosition)
                {
                    player.LastLeftHandXPosition = player.LeftHand.Position.X;
                    Console.WriteLine("Player {0}, LH last XPos{1}]", player.PlayerId, player.LastLeftHandXPosition);

                    if (player.Ready)
                    {
                        player.Fired = true;
                        player.ShootTime = DateTime.Now;
                        CheckState(players);
                    }
                }

            } // for each skeleton

            CheckState(players);
        }

        private List<Player> IdentifyPlayers(SkeletonFrame skeletonframe)
        {
            foreach (var data in skeletonframe.Skeletons)
            {
                if (SkeletonTrackingState.Tracked == data.TrackingState)
                {
                    var data1 = data;
                    var player = _players.FirstOrDefault(x => x.PlayerId == data1.TrackingID);
                    if (player == null)
                    {
                        player = new Player();
                        player.PlayerId = data1.TrackingID;
                        player.Name = "Player " + (_players.Count + 1);
                        player.LastLeftHandXPosition = int.MaxValue;
                        player.LastRightHandXPosition = int.MaxValue;
                        _players.Add(player);
                    }

                    player.LeftHand = data.Joints[JointID.HandLeft];
                    player.LeftElbow = data.Joints[JointID.ElbowLeft];
                    player.RightHand = data.Joints[JointID.HandRight];
                    player.RightElbow = data.Joints[JointID.ElbowRight];
                    player.Head = data.Joints[JointID.Head];
                }
            }
            return _players;
        }

        private void CheckState(IEnumerable<Player> playerPositions)
        {
            Player winner;

            var firedPlayers = playerPositions.Where(x => x.Fired);

            if (firedPlayers.Count() == 1)
            {
                winner = firedPlayers.First();
            }
            else if (firedPlayers.Count() > 1)
            {
                if (firedPlayers.ElementAt(0).ShootTime <= firedPlayers.ElementAt(1).ShootTime)
                {
                    winner = firedPlayers.ElementAt(0);
                }
                else
                {
                    winner = firedPlayers.ElementAt(1);
                }
            }
            else
            {
                return;
            }

            EndGame(winner);
        }

        private void EndGame(Player winner)
        {
            _nui.Uninitialize();
            Winner.Visibility = Visibility.Visible;

            WinnerText.Text = "Winner was " + winner.Name;
        }

        private Point GetDisplayPosition(Joint joint)
        {
            float depthX, depthY;
            _nui.SkeletonEngine.SkeletonToDepthImage(joint.Position, out depthX, out depthY);
            depthX = Math.Max(0, Math.Min(depthX * 320, 320));  //convert to 320, 240 space
            depthY = Math.Max(0, Math.Min(depthY * 240, 240));  //convert to 320, 240 space
            int colorX, colorY;
            var iv = new ImageViewArea();
            // only ImageResolution.Resolution640x480 is supported at this point
            _nui.NuiCamera.GetColorPixelCoordinatesFromDepthPixel(ImageResolution.Resolution640x480, iv, (int)depthX, (int)depthY, 0, out colorX, out colorY);

            // map back to skeleton.Width & skeleton.Height
            return new Point((int)(skeleton.Width * colorX / 640.0), (int)(skeleton.Height * colorY / 480));
        }

        Polyline GetBodySegment(JointsCollection joints, Brush brush, params JointID[] ids)
        {
            var points = new PointCollection(ids.Length);
            for (int i = 0; i < ids.Length; ++i)
            {
                points.Add(GetDisplayPosition(joints[ids[i]]));
            }

            var polyline = new Polyline();
            polyline.Points = points;
            polyline.Stroke = brush;
            polyline.StrokeThickness = 5;
            return polyline;
        }


        private void RenderBlackHat(Point point)
        {
            var image = new Image();
            image.Source = new BitmapImage(new Uri("blackhat_2.png", UriKind.RelativeOrAbsolute));

            skeleton.Children.Add(image);
            Canvas.SetLeft(image, point.X - 75);
            Canvas.SetTop(image, point.Y - 90);
        }

        private void RenderWhiteHat(Point point)
        {
            var image = new Image();
            image.Source = new BitmapImage(new Uri("whitehat_2.png", UriKind.RelativeOrAbsolute));

            skeleton.Children.Add(image);
            Canvas.SetLeft(image, point.X - 75);
            Canvas.SetTop(image, point.Y - 90);

            Console.WriteLine("{0} ; {1}", image.Width, image.Height);
        }

        void HandleSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            var skeletonFrame = e.SkeletonFrame;
            var iSkeleton = 0;
            var brushes = new Brush[6];
            brushes[0] = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            brushes[1] = new SolidColorBrush(Color.FromRgb(0, 255, 0));
            brushes[2] = new SolidColorBrush(Color.FromRgb(64, 255, 255));
            brushes[3] = new SolidColorBrush(Color.FromRgb(255, 255, 64));
            brushes[4] = new SolidColorBrush(Color.FromRgb(255, 64, 255));
            brushes[5] = new SolidColorBrush(Color.FromRgb(128, 128, 255));

            var numberOfPlayer = 0;

            skeleton.Children.Clear();
            foreach (SkeletonData data in skeletonFrame.Skeletons)
            {
                if (SkeletonTrackingState.Tracked == data.TrackingState)
                {
                    // Draw bones
                    Brush brush = brushes[iSkeleton % brushes.Length];
                    skeleton.Children.Add(GetBodySegment(data.Joints, brush, JointID.HipCenter, JointID.Spine, JointID.ShoulderCenter, JointID.Head));
                    skeleton.Children.Add(GetBodySegment(data.Joints, brush, JointID.ShoulderCenter, JointID.ShoulderLeft, JointID.ElbowLeft, JointID.WristLeft, JointID.HandLeft));
                    skeleton.Children.Add(GetBodySegment(data.Joints, brush, JointID.ShoulderCenter, JointID.ShoulderRight, JointID.ElbowRight, JointID.WristRight, JointID.HandRight));
                    skeleton.Children.Add(GetBodySegment(data.Joints, brush, JointID.HipCenter, JointID.HipLeft, JointID.KneeLeft, JointID.AnkleLeft, JointID.FootLeft));
                    skeleton.Children.Add(GetBodySegment(data.Joints, brush, JointID.HipCenter, JointID.HipRight, JointID.KneeRight, JointID.AnkleRight, JointID.FootRight));

                    // Draw joints
                    foreach (Joint joint in data.Joints)
                    {
                        var jointPos = GetDisplayPosition(joint);
                        var jointLine = new Line();
                        jointLine.X1 = jointPos.X - 3;
                        jointLine.X2 = jointLine.X1 + 6;
                        jointLine.Y1 = jointLine.Y2 = jointPos.Y;
                        jointLine.Stroke = _jointColors[joint.ID];
                        jointLine.StrokeThickness = 6;
                        skeleton.Children.Add(jointLine);

                        if (joint.ID == JointID.Head)
                        {
                            if (numberOfPlayer == 0)
                            {
                                RenderBlackHat(jointPos);
                                numberOfPlayer++;
                            }
                            else
                            {
                                RenderWhiteHat(jointPos);
                            }
                        }
                    }
                }
                iSkeleton++;
            } // for each skeleton
        }

        private void HandleWindowClosed(object sender, EventArgs e)
        {
            _nui.Uninitialize();
            Environment.Exit(0);
        }

        private void RestartGame(object sender, RoutedEventArgs e)
        {
            Winner.Visibility = Visibility.Collapsed;
            WinnerText.Text = "";

            InitKinect();
        }
    }

    public class Player
    {
        public float LeftAngle;
        public float RightAngle;
        public float LastLeftHandXPosition;
        public bool Ready;
        public bool Fired;
        public DateTime ShootTime;
        public float LastRightHandXPosition;
        public int PlayerId { get; set; }

        public Joint LeftHand { get; set; }
        public Joint RightHand { get; set; }
        public Joint LeftElbow { get; set; }
        public Joint RightElbow { get; set; }
        public string Name { get; set; }
        public Joint Head { get; set; }
    }
}
