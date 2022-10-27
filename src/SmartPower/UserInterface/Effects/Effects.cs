using Xamarin.Forms;


namespace SmartPower.UserInterface.Effects
{
    public static class Effects
    {
        public const string ResolutionGroupName = "SmartPower.Effects";

        internal static Effect ResolveEffect<TEffect>() where TEffect : RoutingEffect
            => Effect.Resolve(ResolveEffectName<TEffect>());

        internal static string ResolveEffectName<TEffect>() where TEffect : RoutingEffect
            => $"{ResolutionGroupName}.{typeof(TEffect).Name}";  

    }
}
