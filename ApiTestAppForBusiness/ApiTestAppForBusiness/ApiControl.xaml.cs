using System;
using System.Windows;
using System.Windows.Controls;
using Environment = ApiTestAppForBusiness.Model.Environment;

namespace ApiTestAppForBusiness;

public partial class ApiControl : UserControl
{
    private Environment _currentEnvironment;

    public ApiControl()
    {
        InitializeComponent();
    }

    public void SetConfig(Environment currentEnvironment)
    {
        _currentEnvironment = currentEnvironment;
    }
}
