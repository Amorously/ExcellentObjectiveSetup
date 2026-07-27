using EOS.Modules.Tweaks.TerminalTweak;

namespace EOS.BaseClasses.CustomTerminalDefinition
{
    public class TerminalPasswordData
    {
        public bool PasswordProtected { get; set; } = false;

        public string Password { get; set; } = string.Empty;

        public string PasswordHintText { get; set; } = "Password Required.";

        public bool GeneratePassword { get; set; } = true;

        public int PasswordPartCount { get; set; } = 1;

        public bool ShowPasswordLength { get; set; } = false;

        public bool ShowPasswordPartPositions { get; set; } = false;

        public SerialGeneratorManager.CodeWordLength PasswordWordLength { get; set; } = SerialGeneratorManager.CodeWordLength.Four;

        public List<List<CustomTerminalZoneSelectionData>> TerminalZoneSelectionDatas { get; set; } = new() { new() { new() } };

        public TerminalPasswordData()
        {
            PasswordPartCount = Math.Max(1, PasswordPartCount);
        }
    }
}
