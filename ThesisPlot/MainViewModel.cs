// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="OxyPlot">
//   The MIT License (MIT)
//   
//   Copyright (c) 2014 OxyPlot contributors
//   
//   Permission is hereby granted, free of charge, to any person obtaining a
//   copy of this software and associated documentation files (the
//   "Software"), to deal in the Software without restriction, including
//   without limitation the rights to use, copy, modify, merge, publish,
//   distribute, sublicense, and/or sell copies of the Software, and to
//   permit persons to whom the Software is furnished to do so, subject to
//   the following conditions:
//   
//   The above copyright notice and this permission notice shall be included
//   in all copies or substantial portions of the Software.
//   
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//   OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//   MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//   IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//   CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//   TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//   SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace WpfApplication1
{
    using System;

    using OxyPlot;
    using OxyPlot.Series;
    using OxyPlot.Axes;
    using OxyPlot.Annotations;

    using MySql.Data.MySqlClient;

    public class MainViewModel
    {
        static String[] gender = new String[] { "male", "female" };
        static String[] age = new String[] { "16 to 24 years", "25 to 34 years", "35 to 44 years", "45 to 54 years", "55 to 64 years", "65 years and older" };
        static String[] occupation = new String[] { "fully employed", "part-time employed", "self-employed", "in education", "unemployed", "prefer not to say/misc" };
        public MainViewModel()
        {
        }

        public static void InitModel(User user)
        {
            Model = new PlotModel { Title = gender[user.Gender] + ", " + age[user.Age] + ", " + occupation[user.Occupation] };
            Model.LegendBorder = OxyColors.Black;
            Model.LegendOrientation = LegendOrientation.Horizontal;
            Model.LegendPlacement = LegendPlacement.Outside;
            Model.LegendPosition = LegendPosition.TopLeft;

            using (MainWindow.Connection)
            {
                MainWindow.Connection.Open();
                DateTime endTime = user.JoinDate.AddDays(7);
                MySqlCommand command = new MySqlCommand("select max(Date) from accelerometer where IMEI like '" + user.IMEI + "'", MainWindow.Connection);
                endTime = DateTime.Parse(command.ExecuteScalar().ToString());

                DateTimeAxis dateAxis = new DateTimeAxis { Position = AxisPosition.Bottom, AbsoluteMinimum = DateTimeAxis.ToDouble(user.JoinDate), Minimum = DateTimeAxis.ToDouble(user.JoinDate), AbsoluteMaximum = DateTimeAxis.ToDouble(endTime), Maximum = DateTimeAxis.ToDouble(endTime) };
                dateAxis.StringFormat = "MM-dd HH:mm";
                dateAxis.MaximumPadding = 0;
                dateAxis.MinimumPadding = 0;

                Model.Axes.Add(dateAxis);
                LinearAxis linearAxis = new LinearAxis() { Position = AxisPosition.Left, Minimum = 0, Maximum = 4.3, AbsoluteMinimum = 0, AbsoluteMaximum = 4.3 };
                linearAxis.MinorStep = 1;
                linearAxis.MajorStep = 1;
                linearAxis.MaximumPadding = 0;
                linearAxis.MinimumPadding = 0;
                linearAxis.MajorGridlineStyle = LineStyle.Solid;
                Model.Axes.Add(linearAxis);

                LineSeries alarmLine = new LineSeries();
                alarmLine.Color = OxyColors.SkyBlue;
                alarmLine.LineStyle = LineStyle.Dash;
                alarmLine.MarkerFill = OxyColors.SkyBlue;
                alarmLine.MarkerSize = 5;
                alarmLine.MarkerStroke = OxyColors.White;
                alarmLine.MarkerStrokeThickness = 1.5;
                alarmLine.MarkerType = MarkerType.Circle;
                alarmLine.StrokeThickness = 3;
                alarmLine.Title = "Alert Result";

                command = new MySqlCommand("select AlarmID, Time, Result, RingerMode, BatteryLevel, IsCharging from alarm where IMEI like '" + user.IMEI + "' order by Time", MainWindow.Connection);
                using (MySqlDataReader dr = command.ExecuteReader())
                {   
                    System.Collections.ArrayList points = new System.Collections.ArrayList();
                        
                    while (dr.Read())
                    {
                        double date = DateTimeAxis.ToDouble(DateTime.Parse(dr[1].ToString()));
                        double result = Convert.ToDouble(dr[2].ToString());
                        if (result < 0)
                            result = 0;

                        alarmLine.Points.Add(new DataPoint(date, result));

                        String explanation = "normal";
                        OxyColor annotationColor = OxyColor.FromRgb(255, 0, 0);
                        int ringerMode = Convert.ToInt32(dr[3].ToString());
                        if (ringerMode == 0)
                        {
                            annotationColor = OxyColor.FromRgb(0, 255, 0);
                            explanation = "silent";
                        }
                        else if (ringerMode == 1)
                        {
                            annotationColor = OxyColor.FromRgb(127, 0, 0);
                            explanation = "vibrate";
                        }

                        if(dr[5].ToString() == "1")
                            points.Add(new ScatterPoint(date, result));

                        Model.Annotations.Add(new LineAnnotation() { Type = LineAnnotationType.Vertical, X = date, Y = 0, MaximumY = 4, Color = annotationColor, Text = explanation });
                    }

                    Model.Series.Add(alarmLine);

                    ScatterSeries chargingSeries = new ScatterSeries();
                    chargingSeries.Title = "Phone Charging";
                    chargingSeries.MarkerFill = OxyColors.Black;
                    chargingSeries.MarkerSize = 6;
                    chargingSeries.MarkerStroke = OxyColors.Black;
                    chargingSeries.MarkerType = MarkerType.Cross;
                    chargingSeries.ItemsSource = points;
                    Model.Series.Add(chargingSeries);
                }

                LineSeries audioLine = new LineSeries();
                audioLine.Color = OxyColors.Yellow;
                audioLine.Title = "Average Amplitude";
                audioLine.MarkerFill = OxyColors.Yellow;
                audioLine.MarkerSize = 5;
                audioLine.MarkerType = MarkerType.Circle;

                command = new MySqlCommand("select max(AverageAmplitude) from audio", MainWindow.Connection);
                double maxAmplitude = Convert.ToDouble(command.ExecuteScalar().ToString());

                command = new MySqlCommand("select AlarmID, AverageAmplitude from audio where IMEI like '" + user.IMEI + "' and RecordedNoises > 0 order by AlarmID", MainWindow.Connection);
                using (MySqlDataReader dr = command.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        int alarmID = Convert.ToInt32(dr[0].ToString());
                        double dateToUse = alarmID < alarmLine.Points.Count ? alarmLine.Points[alarmID].X : dateAxis.Maximum;
                        audioLine.Points.Add(new DataPoint(dateToUse, Convert.ToDouble(dr[1].ToString()) / maxAmplitude * 4));
                    }

                    Model.Series.Add(audioLine);
                }

                RectangleBarSeries calendarSeries = new RectangleBarSeries();
                calendarSeries.Title = "Calendar Entry";
                calendarSeries.FillColor = OxyColor.FromArgb(99, 0, 0, 255);

                command = new MySqlCommand("select AlarmID, StartTime, EndTime from calendar where IMEI like '" + user.IMEI + "' order by AlarmID", MainWindow.Connection);
                using (MySqlDataReader dr = command.ExecuteReader())
                {
                    int count = 0;
                    while (dr.Read())
                    {
                        int alarmID = Convert.ToInt32(dr[0].ToString());
                        double startTimeCalendar = DateTimeAxis.ToDouble(DateTime.Parse(dr[1].ToString()));
                        double endTimeCalendar = DateTimeAxis.ToDouble(DateTime.Parse(dr[2].ToString()));

                        if (alarmID >= alarmLine.Points.Count)
                            continue;

                        bool duplicate = false;
                        foreach(RectangleBarItem item in calendarSeries.Items)
                        {   
                            if(item.X0 == startTimeCalendar && item.X1 == endTimeCalendar)
                            {
                                duplicate = true;
                                break;
                            }
                        }
                        
                        if(!duplicate)
                        {
                            ++count;
                            calendarSeries.Items.Add(new RectangleBarItem(startTimeCalendar, 3.5 - .1 - .2 * count, endTimeCalendar, 3.5 + .1 - .2 * count));
                        }
                    }

                    Model.Series.Add(calendarSeries);
                }

                LineSeries distanceLine = new LineSeries();
                distanceLine.Color = OxyColors.Violet;
                distanceLine.Title = "Covered Distance";
                distanceLine.MarkerFill = OxyColors.Violet;
                distanceLine.MarkerSize = 5;
                distanceLine.MarkerType = MarkerType.Circle;

                command = new MySqlCommand("select max(Distance) from locations", MainWindow.Connection);
                double maxDistance = Convert.ToDouble(command.ExecuteScalar().ToString());

                command = new MySqlCommand("select AlarmID, Distance from locations where IMEI like '" + user.IMEI + "' order by AlarmID", MainWindow.Connection);
                using (MySqlDataReader dr = command.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        int alarmID = Convert.ToInt32(dr[0].ToString());
                        double dateToUse = alarmID < alarmLine.Points.Count ? alarmLine.Points[alarmID].X : dateAxis.Maximum;
                        distanceLine.Points.Add(new DataPoint(dateToUse, Convert.ToDouble(dr[1].ToString()) / maxDistance * 4));
                    }

                    Model.Series.Add(distanceLine);
                }

                RectangleBarSeries phoneActivitySeries = new RectangleBarSeries();
                phoneActivitySeries.Title = "Phone Activity";
                phoneActivitySeries.FillColor = OxyColors.Black;

                command = new MySqlCommand("select ScreenOnTime, ScreenOffTime from phoneactivity where IMEI like '" + user.IMEI + "' order by ScreenOffTime", MainWindow.Connection);
                using (MySqlDataReader dr = command.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        double screenOnTime = DateTimeAxis.ToDouble(DateTime.Parse(dr[0].ToString()));
                        double screenOffTime = DateTimeAxis.ToDouble(DateTime.Parse(dr[1].ToString()));

                        phoneActivitySeries.Items.Add(new RectangleBarItem(screenOnTime, 4.2, screenOffTime, 4.3));
                    }

                    Model.Series.Add(phoneActivitySeries);
                }

                ScatterSeries phoneCallSeries = new ScatterSeries();
                phoneCallSeries.Title = "Phone Calls";
                phoneCallSeries.MarkerFill = OxyColors.White;
                phoneCallSeries.MarkerSize = 3;
                phoneCallSeries.MarkerStroke = OxyColors.DarkSeaGreen;
                phoneCallSeries.MarkerType = MarkerType.Circle;

                command = new MySqlCommand("select EndTime from phonecalls where IMEI like '" + user.IMEI + "' order by EndTime", MainWindow.Connection);
                using (MySqlDataReader dr = command.ExecuteReader())
                {
                    System.Collections.ArrayList points = new System.Collections.ArrayList();
                    while (dr.Read())
                    {
                        double callTime = DateTimeAxis.ToDouble(DateTime.Parse(dr[0].ToString()));
                        points.Add(new ScatterPoint(callTime, 4.12));
                    }

                    phoneCallSeries.ItemsSource = points;
                    Model.Series.Add(phoneCallSeries);
                }

                ScatterSeries messagesSeries = new ScatterSeries();
                messagesSeries.Title = "Text Messages";
                messagesSeries.MarkerFill = OxyColors.White;
                messagesSeries.MarkerSize = 3;
                messagesSeries.MarkerStroke = OxyColors.DarkViolet;
                messagesSeries.MarkerType = MarkerType.Circle;

                command = new MySqlCommand("select Time from sms where IMEI like '" + user.IMEI + "' order by Time", MainWindow.Connection);
                using (MySqlDataReader dr = command.ExecuteReader())
                {
                    System.Collections.ArrayList points = new System.Collections.ArrayList();
                    while (dr.Read())
                    {
                        double time = DateTimeAxis.ToDouble(DateTime.Parse(dr[0].ToString()));
                        points.Add(new ScatterPoint(time, 4.07));
                    }

                    messagesSeries.ItemsSource = points;
                    Model.Series.Add(messagesSeries);
                }

                LineSeries accelerometerLine = new LineSeries();
                accelerometerLine.Color = OxyColors.Brown;
                accelerometerLine.Title = "Accelerometer Distance";
                accelerometerLine.StrokeThickness = .5;

                command = new MySqlCommand("select max(AverageDistance) from accelerometer", MainWindow.Connection);
                maxDistance = Convert.ToDouble(command.ExecuteScalar().ToString());

                command = new MySqlCommand("select Date, AverageDistance from accelerometer where IMEI like '" + user.IMEI + "' order by Date", MainWindow.Connection);
                using (MySqlDataReader dr = command.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        double time = DateTimeAxis.ToDouble(DateTime.Parse(dr[0].ToString()));
                        accelerometerLine.Points.Add(new DataPoint(time, Convert.ToDouble(dr[1].ToString()) / maxDistance * 4));
                    }

                    Model.Series.Add(accelerometerLine);
                }
            }
        }

        public static void SwitchVisibility(bool showAmplitude, bool showDistance, bool showAccelerometer)
        {
            // alarm, charging, audio, calendar, distance, activity, call, messages, accelerometer
            Model.Series[2].IsVisible = showAmplitude;
            Model.Series[4].IsVisible = showDistance;
            Model.Series[8].IsVisible = showAccelerometer;
        }

        public static PlotModel Model { get; private set; }
    }
}