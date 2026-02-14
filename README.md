# My C# Music Player Project

This is a WPF-based music player I developed for my software workshop. It's more than just a basic player—it actually connects to the web to find info about your songs.

### What it can do:
* **Smart Search:** When you play a song, it automatically talks to the iTunes API to grab the album cover and the correct artist name.
* **Full Control:** I added a "Details" window where you can manually fix song titles or album names if the internet doesn't find them.
* **Storage:** All your music library data is kept in a JSON file, so it remembers everything even after you close the app.
* **Smooth UI:** The app handles internet requests in the background so the interface never freezes while searching.

### The Tech Side:
I used **C#** with **WPF** for the design. For the data, I used **Newtonsoft.Json**, and for the internet connection, I used **HttpClient** with async/await logic and cancellation tokens to make sure the UI stays responsive.
