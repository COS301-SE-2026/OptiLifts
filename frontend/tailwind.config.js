/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      fontFamily: {
        display: ["Bebas Neue", "sans-serif"],
        sans: ["Barlow", "sans-serif"],
      },
      letterSpacing: {
        wide: "0.025em",
      },
    },
  },
  plugins: [],
}
