/** Fundo decorativo global — aurora + grid perspectiva (puramente estético). */
export function NexusBackground() {
  return (
    <div
      className="pointer-events-none fixed inset-0 -z-10 overflow-hidden bg-background"
      aria-hidden="true"
    >
      <div
        className="nexus-bg-animate absolute -inset-[20%] opacity-70"
        style={{
          background:
            'radial-gradient(ellipse 80% 60% at 20% 20%, rgba(74,134,255,0.35), transparent 55%), radial-gradient(ellipse 70% 50% at 80% 10%, rgba(120,80,255,0.25), transparent 50%), radial-gradient(ellipse 60% 70% at 50% 100%, rgba(242,187,90,0.12), transparent 45%)',
          animation: 'nexus-aurora-shift 28s ease-in-out infinite',
        }}
      />

      <div
        className="nexus-bg-animate absolute -left-1/4 top-1/4 size-[55vmin] rounded-full bg-primary/25 blur-3xl"
        style={{ animation: 'nexus-orb-drift 22s ease-in-out infinite' }}
      />
      <div
        className="nexus-bg-animate absolute -right-1/4 top-1/3 size-[45vmin] rounded-full bg-brand-violet/20 blur-3xl"
        style={{ animation: 'nexus-orb-drift 26s ease-in-out infinite reverse' }}
      />
      <div
        className="nexus-bg-animate absolute bottom-0 left-1/3 size-[40vmin] rounded-full bg-warning/10 blur-3xl"
        style={{ animation: 'nexus-orb-drift 30s ease-in-out infinite 4s' }}
      />

      <div
        className="nexus-bg-animate absolute inset-0 opacity-40"
        style={{
          backgroundImage:
            'linear-gradient(rgba(132,162,220,0.08) 1px, transparent 1px), linear-gradient(90deg, rgba(132,162,220,0.08) 1px, transparent 1px)',
          backgroundSize: '48px 48px',
          maskImage: 'radial-gradient(ellipse 85% 75% at 50% 40%, black 20%, transparent 75%)',
          WebkitMaskImage: 'radial-gradient(ellipse 85% 75% at 50% 40%, black 20%, transparent 75%)',
          animation: 'nexus-grid-pulse 8s ease-in-out infinite',
        }}
      />
    </div>
  );
}
