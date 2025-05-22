namespace View.Personal.Classes
{
    /// <summary>
    /// Represents a single onboarding step, including target UI element and descriptive details.
    /// </summary>
    public class OnboardingStep
    {
        /// <summary>
        /// Gets or sets the name of the control to be highlighted during the onboarding step.
        /// </summary>
        public string TargetName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the title displayed in the onboarding tooltip.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description displayed in the onboarding tooltip.
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}
