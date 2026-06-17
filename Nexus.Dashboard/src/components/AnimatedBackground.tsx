/** Fundo decorativo global — malha de rede / fluxo operacional (puramente estético). */
export function AnimatedBackground() {
  return (
    <div className="nexus-bg" aria-hidden="true">
      <div className="nexus-bg__gradient" />
      <div className="nexus-bg__hex" />
      <div className="nexus-bg__noise" />

      <svg
        className="nexus-bg__mesh"
        viewBox="0 0 1920 1080"
        preserveAspectRatio="xMidYMid slice"
        xmlns="http://www.w3.org/2000/svg"
      >
        <defs>
          <linearGradient id="nexus-edge-blue" x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%" stopColor="#4a86ff" stopOpacity="0" />
            <stop offset="45%" stopColor="#4a86ff" stopOpacity="0.55" />
            <stop offset="100%" stopColor="#4a86ff" stopOpacity="0" />
          </linearGradient>
          <linearGradient id="nexus-edge-gold" x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%" stopColor="#f2bb5a" stopOpacity="0" />
            <stop offset="50%" stopColor="#f2bb5a" stopOpacity="0.65" />
            <stop offset="100%" stopColor="#f2bb5a" stopOpacity="0" />
          </linearGradient>
          <filter id="nexus-glow" x="-50%" y="-50%" width="200%" height="200%">
            <feGaussianBlur stdDeviation="3" result="blur" />
            <feMerge>
              <feMergeNode in="blur" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
        </defs>

        <g className="nexus-bg__links" strokeWidth="1">
          <line x1="960" y1="180" x2="620" y2="360" />
          <line x1="960" y1="180" x2="960" y2="380" />
          <line x1="960" y1="180" x2="1300" y2="360" />
          <line x1="620" y1="360" x2="420" y2="560" />
          <line x1="620" y1="360" x2="720" y2="580" />
          <line x1="960" y1="380" x2="820" y2="590" />
          <line x1="960" y1="380" x2="1100" y2="590" />
          <line x1="1300" y1="360" x2="1180" y2="560" />
          <line x1="1300" y1="360" x2="1500" y2="580" />
          <line x1="420" y1="560" x2="280" y2="760" />
          <line x1="720" y1="580" x2="640" y2="780" />
          <line x1="820" y1="590" x2="900" y2="790" />
          <line x1="1100" y1="590" x2="1040" y2="780" />
          <line x1="1180" y1="560" x2="1280" y2="770" />
          <line x1="1500" y1="580" x2="1620" y2="760" />
          <line x1="720" y1="580" x2="1100" y2="590" strokeOpacity="0.35" />
          <line x1="620" y1="360" x2="1300" y2="360" strokeOpacity="0.25" />
        </g>

        <g className="nexus-bg__nodes" filter="url(#nexus-glow)">
          <circle className="nexus-bg__node nexus-bg__node--hub" cx="960" cy="180" r="7" />
          <circle className="nexus-bg__node nexus-bg__node--relay" cx="620" cy="360" r="5" />
          <circle className="nexus-bg__node nexus-bg__node--relay" cx="960" cy="380" r="5" />
          <circle className="nexus-bg__node nexus-bg__node--relay" cx="1300" cy="360" r="5" />
          <circle className="nexus-bg__node nexus-bg__node--cell" cx="420" cy="560" r="4" />
          <circle className="nexus-bg__node nexus-bg__node--cell" cx="720" cy="580" r="4" />
          <circle className="nexus-bg__node nexus-bg__node--cell" cx="820" cy="590" r="4" />
          <circle className="nexus-bg__node nexus-bg__node--cell" cx="1100" cy="590" r="4" />
          <circle className="nexus-bg__node nexus-bg__node--cell" cx="1180" cy="560" r="4" />
          <circle className="nexus-bg__node nexus-bg__node--cell" cx="1500" cy="580" r="4" />
          <circle className="nexus-bg__node nexus-bg__node--terminal" cx="280" cy="760" r="3" />
          <circle className="nexus-bg__node nexus-bg__node--terminal" cx="640" cy="780" r="3" />
          <circle className="nexus-bg__node nexus-bg__node--terminal" cx="900" cy="790" r="3" />
          <circle className="nexus-bg__node nexus-bg__node--terminal" cx="1040" cy="780" r="3" />
          <circle className="nexus-bg__node nexus-bg__node--terminal" cx="1280" cy="770" r="3" />
          <circle className="nexus-bg__node nexus-bg__node--terminal" cx="1620" cy="760" r="3" />
        </g>

        <g className="nexus-bg__flows">
          <circle className="nexus-bg__packet nexus-bg__packet--gold" r="2.5">
            <animateMotion dur="34s" repeatCount="indefinite" path="M960,180 L620,360 L420,560 L280,760" />
          </circle>
          <circle className="nexus-bg__packet nexus-bg__packet--blue" r="2">
            <animateMotion dur="28s" repeatCount="indefinite" path="M960,180 L960,380 L900,790" />
          </circle>
          <circle className="nexus-bg__packet nexus-bg__packet--gold" r="2.5">
            <animateMotion dur="38s" repeatCount="indefinite" begin="4s" path="M960,180 L1300,360 L1500,580 L1620,760" />
          </circle>
          <circle className="nexus-bg__packet nexus-bg__packet--blue" r="2">
            <animateMotion dur="32s" repeatCount="indefinite" begin="9s" path="M620,360 L720,580 L1100,590 L1040,780" />
          </circle>
        </g>
      </svg>

      <div className="nexus-bg__orb nexus-bg__orb--gold" />
      <div className="nexus-bg__orb nexus-bg__orb--blue" />
      <div className="nexus-bg__orb nexus-bg__orb--violet" />
      <div className="nexus-bg__scan" />
      <div className="nexus-bg__vignette" />
    </div>
  );
}
