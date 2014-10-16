// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="OxyPlot">
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
    using System.Windows;
    using MySql.Data.MySqlClient;

    public class User
    {
        public String IMEI { get; set; }
        public String EMail { get; set; }
        public String CountryCode { get; set; }
        public DateTime JoinDate { get; set; }
        public int Age { get; set; }
        public int Occupation { get; set; }
        public int Gender { get; set; }

        public User(String imei, String email, String countryCode, String joinDate, String age, String occupation, String gender)
        {
            IMEI = imei;
            EMail = email;
            CountryCode = countryCode;
            JoinDate = DateTime.Parse(joinDate);
            Age = Convert.ToInt32(age);
            Occupation = Convert.ToInt32(occupation);
            Gender = Convert.ToInt32(gender);
        }

        public override string ToString()
        {
            return IMEI;
        }
    }

    public partial class MainWindow : Window
    {
        public static MySqlConnection Connection = new MySqlConnection("Server=localhost;userid=root;password=;Database=thesis");

        public MainWindow()
        {
            InitializeComponent();
            
            using(Connection)
            {
                Connection.Open();
                MySqlCommand command = new MySqlCommand("select IMEI, EMail, CountryCode, JoinDate, Age, Occupation, Gender from user", Connection);
                using (MySqlDataReader dr = command.ExecuteReader())
                    while (dr.Read())
                        comboBoxUsers.Items.Add(new User(dr[0].ToString(), dr[1].ToString(), dr[2].ToString(), dr[3].ToString(), dr[4].ToString(), dr[5].ToString(), dr[6].ToString()));
            }

            comboBoxUsers.SelectedIndex = 0;            
        }

        private void comboBoxUsers_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            MainViewModel.InitModel(((User)comboBoxUsers.SelectedItem));
            plotView.Model = MainViewModel.Model;
            buttonPrevious.IsEnabled = comboBoxUsers.SelectedIndex != 0;
            buttonNext.IsEnabled = comboBoxUsers.SelectedIndex != comboBoxUsers.Items.Count - 1;
        }

        private void buttonPrevious_Click(object sender, RoutedEventArgs e)
        {
            buttonNext.IsEnabled = true;
            --comboBoxUsers.SelectedIndex;

            buttonPrevious.IsEnabled = comboBoxUsers.SelectedIndex != 0;
        }

        private void buttonNext_Click(object sender, RoutedEventArgs e)
        {
            buttonPrevious.IsEnabled = true;
            ++comboBoxUsers.SelectedIndex;

            buttonNext.IsEnabled = comboBoxUsers.SelectedIndex != comboBoxUsers.Items.Count - 1;
        }
    }
}