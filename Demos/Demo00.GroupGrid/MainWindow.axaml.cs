namespace Demo00.GroupGrid;

public partial class MainWindow : Window
{
    // ● private fields
    private bool fIsWindowInitialized;
    
    // ● private
    /// <summary>
    /// Initializes the window after it is opened.
    /// </summary>
    private void WindowInitialize()
    {
 
    }
    
    // ● protected
    /// <summary>
    /// Handles the window opened event.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (fIsWindowInitialized)
            return;

        WindowInitialize();
        fIsWindowInitialized = true;
        //LogBox.AppendLine("Application Started.");
    }
 

    // ● constructor
    public MainWindow()
    {
        InitializeComponent();
    }
}