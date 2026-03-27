/* --------------------------------------------------------------
   NMSE.Site - JavaScript
   • Animated star-field background (NMS spectral-class colours)
   • Auto-populates the download link from the latest GitHub
     Release so users get a direct .zip download without needing
     to navigate the GitHub Actions page (with CI fallback).
   • Configurable: update SITE_CONFIG
   -------------------------------------------------------------- */

// -- Portable configuration ------------------------------------ */
const SITE_CONFIG = {
  owner: "vectorcmdr",                                                         // GitHub user / org
  repo: "NMSE",                                                                // Repository name
  releaseTag: "latest",                                                        // LEGACY – no longer used (kept for reference)
  workflowFile: "build-nmse.yml",                                              // Workflow that produces the artifact (fallback)
  discordInvite: "https://discord.gg/WbDQKKP3us",                              // Discord invite link
  userdoc: "https://github.com/vectorcmdr/NMSE/blob/main/docs/user/README.md", // Path to user guide documentation in the repo
  devdoc: "https://github.com/vectorcmdr/NMSE/blob/main/docs/dev/README.md",   // Path to developer documentation in the repo
};

// -- Helpers ---------------------------------------------------- */
function timeAgo(dateString) {
  const seconds = Math.floor((Date.now() - new Date(dateString).getTime()) / 1000);
  const intervals = [
    { label: "year",   secs: 31536000 },
    { label: "month",  secs: 2592000 },
    { label: "day",    secs: 86400 },
    { label: "hour",   secs: 3600 },
    { label: "minute", secs: 60 },
  ];
  for (const { label, secs } of intervals) {
    const count = Math.floor(seconds / secs);
    if (count >= 1) return `${count} ${label}${count > 1 ? "s" : ""} ago`;
  }
  return "just now";
}

// -- Download link + build info --------------------------------- */
(function initDownloadLink() {
  const winLink = document.getElementById("download-link-windows");
  const linuxLink = document.getElementById("download-link-linux");
  const note = document.getElementById("download-note");
  const buildInfo = document.getElementById("build-info");
  const buildNumber = document.getElementById("build-number");
  const buildUpdated = document.getElementById("build-updated");

  // SVG icons used for the download buttons (kept as strings so they can be
  // injected into button innerHTML and inherit currentColor)
  const WINDOWS_SVG = `
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
      <path d="M2 3.5L11 2v8H2z" fill="currentColor"/>
      <path d="M13 2l9-1v8h-9z" fill="currentColor" opacity="0.95"/>
      <path d="M2 13l9-1v8L2 20z" fill="currentColor" opacity="0.9"/>
      <path d="M13 12l9-1v8l-9 1z" fill="currentColor" opacity="0.85"/>
    </svg>`;

  const LINUX_SVG = `
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" aria-hidden="true" focusable="false">
      <!-- Black body -->
      <path d="M32 6c-11 0-20 9-20 20 0 7 4 13 8 17 4 4 10 7 12 7s8-3 12-7c4-4 8-10 8-17 0-11-9-20-20-20z" fill="#000000"/>
      <!-- White belly -->
      <ellipse cx="32" cy="34" rx="10" ry="14" fill="#FFFFFF"/>
      <!-- Face (white) -->
      <ellipse cx="32" cy="22" rx="6" ry="5" fill="#FFFFFF"/>
      <!-- Eyes (black) -->
      <circle cx="29" cy="21" r="1.2" fill="#000000"/>
      <circle cx="35" cy="21" r="1.2" fill="#000000"/>
      <!-- Beak (yellow) -->
      <path d="M32 24.5 L28.5 27 L35.5 27 Z" fill="#FFCC33"/>
      <!-- Feet (yellow) -->
      <ellipse cx="26" cy="48" rx="3.5" ry="2" fill="#FFCC33"/>
      <ellipse cx="38" cy="48" rx="3.5" ry="2" fill="#FFCC33"/>
    </svg>`;

  const WINDOWS_BUTTON_HTML = WINDOWS_SVG + ' <b>Download</b>';
  const LINUX_BUTTON_HTML = LINUX_SVG + ' <b>Download</b>';

  // Fallback: send users to the Actions page if the release API fails
  const actionsUrl =
    `https://github.com/${SITE_CONFIG.owner}/${SITE_CONFIG.repo}/actions/workflows/${SITE_CONFIG.workflowFile}`;
  const fallbackNote = "Opens the build workflow page \u2013 download the artifact from there.";

  // Default to the Actions page so buttons are never dead links
  if (winLink) {
    winLink.href = actionsUrl;
    // Use the shared constant defined above
    winLink.innerHTML = WINDOWS_BUTTON_HTML;
  }
  if (linuxLink) {
    linuxLink.href = actionsUrl;
    linuxLink.innerHTML = LINUX_BUTTON_HTML;
  }

  // Fetch the latest GitHub Release (created by CI with a versioned tag e.g. v1.2.3)
  const apiUrl =
    `https://api.github.com/repos/${SITE_CONFIG.owner}/${SITE_CONFIG.repo}/releases/latest`;

  fetch(apiUrl)
    .then(res => {
      if (!res.ok) throw new Error(res.status);
      return res.json();
    })
    .then(release => {
      if (release.assets && release.assets.length > 0) {
        // Prefer a .zip for Windows and an .AppImage for Linux (case-insensitive)
        const assets = release.assets;
        const zipAsset = assets.find(a => /\.zip$/i.test(a.name) || /\.zip$/i.test(a.browser_download_url));
        const appImageAsset = assets.find(a => /\.appimage$/i.test(a.name) || /\.appimage$/i.test(a.browser_download_url));

        if (winLink) {
          if (zipAsset) {
            winLink.href = zipAsset.browser_download_url;
            winLink.innerHTML = WINDOWS_BUTTON_HTML;
            winLink.title = zipAsset.name;
          } else {
            // No zip found - use first asset as fallback but keep users informed
            winLink.href = assets[0].browser_download_url;
            winLink.innerHTML = WINDOWS_BUTTON_HTML;
            winLink.title = assets[0].name;
          }
        }

        if (linuxLink) {
          if (appImageAsset) {
            linuxLink.href = appImageAsset.browser_download_url;
            linuxLink.innerHTML = LINUX_BUTTON_HTML;
            linuxLink.title = appImageAsset.name;
          } else {
            // No AppImage found - keep it pointing to actions page as a fallback and set an explanatory title
            linuxLink.href = actionsUrl;
            linuxLink.innerHTML = LINUX_BUTTON_HTML;
            linuxLink.title = "No Linux AppImage available in this release";
          }
        }

        // Show the version from the release name or tag (e.g. "NMSE v1.2.3" or "v1.2.3")
        buildNumber.textContent = release.name || release.tag_name;
        buildUpdated.textContent = `updated ${timeAgo(release.published_at)}`;
        buildInfo.hidden = false;
        note.textContent = "Click download above for your OS.";
      } else {
        note.textContent = "No builds available yet.";
      }
    })
    .catch(() => {
      // Release not found - fall back to Actions page
      if (winLink) winLink.href = actionsUrl;
      if (linuxLink) linuxLink.href = actionsUrl;
      note.textContent = fallbackNote;
    });
})();

// -- Discord link ----------------------------------------------- */
(function initDiscordLink() {
  const link = document.getElementById("discord-link");
  link.href = SITE_CONFIG.discordInvite;
})();

// -- Developer links -------------------------------------------- */
(function initDevLinks() {
  const ghLink = document.getElementById("dev-github-link");
  const sponsorLink = document.getElementById("dev-sponsor-link");
  const repoLink = document.getElementById("dev-like-link");
  const issueLink = document.getElementById("dev-issue-link");
  const footerLink = document.getElementById("dev-github-link-footer");
  const docsUserLink = document.getElementById("docs-user-link");
  const docsDevLink = document.getElementById("docs-dev-link");

  ghLink.href = `https://github.com/${SITE_CONFIG.owner}`;
  ghLink.textContent = SITE_CONFIG.owner;

  sponsorLink.href = `https://github.com/sponsors/${SITE_CONFIG.owner}`;

  repoLink.href = `https://github.com/${SITE_CONFIG.owner}/${SITE_CONFIG.repo}`;

  issueLink.href = `https://github.com/${SITE_CONFIG.owner}/${SITE_CONFIG.repo}/issues`;

  footerLink.href = `https://github.com/${SITE_CONFIG.owner}`;
  footerLink.textContent = SITE_CONFIG.owner;

  docsUserLink.href = `${SITE_CONFIG.userdoc}`;
  docsDevLink.href = `${SITE_CONFIG.devdoc}`;
})();

// -- Star-field (NMS spectral-class colours) -------------------- */
(function initStarfield() {
  const canvas = document.getElementById("starfield");
  if (!canvas) return;
  const ctx = canvas.getContext("2d");

  // NMS stellar spectral-class colours
  const STAR_COLOURS = [
    [107, 136, 255],  // O - Blue
    [160, 196, 255],  // B - Blue-white
    [240, 240, 255],  // A - White
    [255, 248, 224],  // F - Yellow-white
    [255, 229, 102],  // G - Yellow
    [255, 170,  51],  // K - Orange
    [255, 102,  51],  // M - Red
    [ 51, 255, 153],  // E - Green (exotic)
  ];

  let width, height, stars;
  const STAR_COUNT = 260;
  const MAX_DEPTH = 600;
  const STAR_SPEED = 0.15;

  function resize() {
    width = canvas.width = window.innerWidth;
    height = canvas.height = window.innerHeight;
  }

  function createStars() {
    stars = Array.from({ length: STAR_COUNT }, () => ({
      x: Math.random() * width - width / 2,
      y: Math.random() * height - height / 2,
      z: Math.random() * MAX_DEPTH,
      colour: STAR_COLOURS[Math.floor(Math.random() * STAR_COLOURS.length)],
    }));
  }

  function draw() {
    ctx.clearRect(0, 0, width, height);

    for (const s of stars) {
      s.z -= STAR_SPEED;
      if (s.z <= 0) {
        s.x = Math.random() * width - width / 2;
        s.y = Math.random() * height - height / 2;
        s.z = MAX_DEPTH;
        s.colour = STAR_COLOURS[Math.floor(Math.random() * STAR_COLOURS.length)];
      }

      const scale = 128 / s.z;
      const sx = s.x * scale + width / 2;
      const sy = s.y * scale + height / 2;
      const r = Math.max(0, 1.2 - s.z / MAX_DEPTH) * 1.5;
      const alpha = Math.max(0, 1 - s.z / MAX_DEPTH);

      const [cr, cg, cb] = s.colour;
      ctx.beginPath();
      ctx.arc(sx, sy, r, 0, Math.PI * 2);
      ctx.shadowBlur = r * 8;
      ctx.shadowColor = `rgba(${cr},${cg},${cb},${(alpha * 2).toFixed(2)})`;
      ctx.fillStyle = `rgba(${cr},${cg},${cb},${(alpha * 1.6).toFixed(2)})`;
      ctx.fill();
    }

    ctx.shadowBlur = 0;
    ctx.shadowColor = "transparent";

    requestAnimationFrame(draw);
  }

  window.addEventListener("resize", () => {
    resize();
    createStars();
  });

  resize();
  createStars();
  draw();
})();
