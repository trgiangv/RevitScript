namespace RevitScript.Runtime.Engine {
    public class NotSupportedFeatureException : Exception {
        public static string NotSupportedMessage = "This feature is not supported under this Revit version.";

        public NotSupportedFeatureException() { }

        public override string Message => NotSupportedMessage;
    }
}

