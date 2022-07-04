using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ApiTestAppForBusiness.Model;
using Microsoft.Extensions.Configuration;
using Environment = ApiTestAppForBusiness.Model.Environment;

namespace ApiTestAppForBusiness;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ApiConfiguration _apiConfiguration;
    public Environment CurrentEnvironment { get; set; }

    public MainWindow()
    {
        InitializeComponent();

        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.edit.json", optional: true)
            .AddJsonFile("appsettings.powerbi.json", optional: true)
            .Build();

        _apiConfiguration = ApiConfiguration.GetInstance(config);
        CurrentEnvironment = _apiConfiguration.Environments.First();

        ApiControl.SetConfig(CurrentEnvironment);
    }

}
