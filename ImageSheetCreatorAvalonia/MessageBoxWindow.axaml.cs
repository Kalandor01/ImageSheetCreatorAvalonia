using Avalonia.Controls;

namespace ImageSheetCreatorAvalonia;

public partial class MessageBoxWindow : Window
{
    public MessageBoxWindow(string message)
    {
        InitializeComponent();
        MessageTextBlock.Text = message;
        OkButton.Click += OkButton_Click;
        Width = message.Length * 7.5 + 50;
        Height = 150;
    }

    public MessageBoxWindow()
        :this("")
    {

    }

    private void OkButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}