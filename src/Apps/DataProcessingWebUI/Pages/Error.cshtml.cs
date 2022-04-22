namespace DataProcessingWebUI.Pages
{

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        private readonly ILogger<ErrorModel> _logger;

        public ErrorModel(ILogger<ErrorModel> logger)
        {
            this._logger = logger;
        }

        public string? RequestId { get; set; }

        public bool ShowRequestId
        {
            get { return !string.IsNullOrEmpty(this.RequestId); }
        }

        public void OnGet()
        {
            this.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        }
    }

}