namespace EOS.BaseClasses.CustomTerminalDefinition
{
    public class TerminalPasswordData
    {
        public bool PasswordProtected { set; get; } = false;

        public string Password { set; get; } = string.Empty;

        public string PasswordHintText { set; get; } = "Password Required.";

        public bool GeneratePassword { set; get; } = true;

        public int PasswordPartCount { set; get; } = 1;

        public bool ShowPasswordLength { set; get; } = false;

        public bool ShowPasswordPartPositions { set; get; } = false;

        public List<List<CustomTerminalZoneSelectionData>> TerminalZoneSelectionDatas { set; get; } = new() { new() { new() } };

        public TerminalPasswordData()
        {
            // TODO: debug this
            PasswordPartCount = Math.Max(1, PasswordPartCount);
        }
    }

}
