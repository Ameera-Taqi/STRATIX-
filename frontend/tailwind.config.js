/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'class',
  content: ['./src/**/*.{html,ts}'],
  theme: {
    extend: {
      colors: {
        primary: '#2563EB',
        dark: '#0F172A',
        surface: '#F8FAFC',
        success: '#22C55E',
        warning: '#F59E0B',
        danger: '#EF4444',
      },
    },
  },
  plugins: [],
};
