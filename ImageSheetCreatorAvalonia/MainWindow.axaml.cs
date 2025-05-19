using Avalonia.Controls;
using System;

namespace ImageSheetCreatorAvalonia;

public partial class MainWindow : Window
{
    #region Public constructors
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        var viewModel = (MainViewModel)DataContext!;
        viewModel.Setup(this);
    }
    #endregion
}