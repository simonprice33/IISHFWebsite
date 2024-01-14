namespace IISHFTest.Core.Configurations
{
    public class EmailConfiguration
    {
        public Uri IishfLogoPath { get; set; } = new Uri("https://iishf.com/images/logos/IISHFLogo_300dpi.png");

        public Uri EmailTemplateBaseUrl { get; set; } =
            new Uri("https://iishf.blob.core.windows.net/templates/TeamInvitationToEvent.html");

        public string NoReplyEmailAdddress { get; set; } = "events@iishf.com";

        public string DisplayName { get; set; } = "IISHF Events";
    }
}
