/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'class',
  content: ['./src/**/*.{html,ts}'],
  theme: {
    extend: {
      colors: {
        primary: '#3060D0',
        dark: '#0F172A',
        surface: '#F8FAFC',
        success: '#4E9B92',
        warning: '#E0B45C',
        danger: '#E07A7A',
        chart: {
          blue: '#3060D0',
          purple: '#ACACF4',
          teal: '#4E9B92',
          amber: '#E0B45C',
          coral: '#E07A7A',
        },
        // Premium dark palette (navy-black) — overrides the deep slate shades so the
        // whole dark theme adopts the reference look without touching every component.
        slate: {
          700: '#232c3d',
          800: '#141a26',
          900: '#0d121e',
          950: '#080b13',
        },
      },
      backgroundImage: {
        'grid-dots': 'radial-gradient(rgba(148,163,184,0.12) 1px, transparent 1px)',
      },
      backgroundSize: {
        'grid-dots': '22px 22px',
      },
    },
  },
  plugins: [],
};
