/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        brand: {
          50: '#f0fdfd',
          100: '#ccfbfb',
          200: '#99f6f7',
          300: '#5eead4',
          400: '#2dd4bf',
          500: '#00B1BA', // QueueSense Primary Brand Color
          600: '#009ca4',
          700: '#007e85',
          800: '#04646a',
          900: '#085358',
        }
      }
    },
  },
  plugins: [],
}
