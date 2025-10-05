/** Copyright © licensed - ยัวร์วิชั่นฯ (https://yourvisioninfo.com), all rights reserved. */

class bstheme {
  "use strict";
  constructor() {
    this.PREFERED_SCHEME = "(prefers-color-scheme: dark)";
    const storedTheme = bstheme.StorageTheme;

    const getPreferredTheme = () => {
      if (storedTheme) {
        return storedTheme;
      }

      return window.matchMedia(this.PREFERED_SCHEME).matches ? "dark" : "light";
    };

    const setTheme = (theme) => {
      if (
        theme === "auto" &&
        window.matchMedia(this.PREFERED_SCHEME).matches
      ) {
        bstheme.Theme = "dark";
      } else {
        bstheme.Theme = theme;
      }
      bstheme.StorageTheme = theme;
      document.dispatchEvent(new Event('bsthemechange'));
    };

    setTheme(getPreferredTheme());

    window
      .matchMedia(this.PREFERED_SCHEME)
      .addEventListener("change", () => {
        if (storedTheme !== "light" || storedTheme !== "dark") {
          setTheme(getPreferredTheme());
        }
      });

    window.addEventListener("DOMContentLoaded", () => {
      bstheme.updateIconTheme(getPreferredTheme());
      const togglers = document.querySelectorAll(`[${bstheme.THEME_TOGGLERS}]`);
      togglers?.forEach(toggler => {
        toggler.addEventListener("click", () => {
          const theme = bstheme.Theme != "dark" ? "dark" : "light";
          setTheme(theme);
          bstheme.updateIconTheme(theme);
        });
      });
    });
  }
  static get THEME_TEMPLATE() {
    return "bootstrap-template-theme";
  }
  static get THEME_TOGGLERS() {
    return "data-toggle-theme";
  }
  static get Theme() {
    return document.documentElement.getAttribute("data-bs-theme") ?? this.StorageTheme ?? "light";
  }
  static set Theme(theme) {
    document.documentElement.setAttribute("data-bs-theme", theme);
  }
  static get IsDark() {
    return this.Theme.toLowerCase() == "dark";
  }
  static get StorageTheme() {
    return localStorage.getItem(this.THEME_TEMPLATE);
  }
  static set StorageTheme(theme) {
    localStorage.setItem(this.THEME_TEMPLATE, theme);
  }
  static updateIconTheme(theme) { }
  static updateIconTheme_example(theme) {
    const initThemeIcon = "init-theme-icon";
    if (!theme) {
      theme = this.Theme;
      localStorage.setItem(initThemeIcon, true);
    } else if (localStorage.getItem(initThemeIcon)) {
      localStorage.removeItem(initThemeIcon);
      return;
    }

    const doms = document.querySelectorAll(`[${bstheme.THEME_TOGGLERS}]`);
    doms?.forEach(dom => {
      if (theme == "dark") {
        dom.classList.add("text-light");
        dom.classList.remove("text-dark");
      } else {
        dom.classList.add("text-dark");
        dom.classList.remove("text-light");
      }
      if (dom.classList.contains('d-none')) {
        dom.classList.remove("d-none");
      }
    });
  }
}

class fullscreen {
  static get FULLSCREENTOGGLER() { return '[data-toggle-fullscreen]'; }
  static get MAXIMIZEICON() { return '[data-fullscreen-icon="maximize"]'; }
  static get MINIMIZEICON() { return '[data-fullscreen-icon="minimize"]'; }
  constructor() {
    // Add event listener for fullscreen change
    document.addEventListener("fullscreenchange", this.onFullscreenChange);
    // For vendor prefixes (older browsers)
    document.addEventListener("webkitfullscreenchange", this.onFullscreenChange);
    document.addEventListener("mozfullscreenchange", this.onFullscreenChange);
    document.addEventListener("MSFullscreenChange", this.onFullscreenChange);
    document.addEventListener("keydown", e => {
      if (e.key?.toLowerCase() == 'f11') {
        e.preventDefault();
        document.activeElement.blur();
        this.toggleFullScreen();
      }
    });
    window.addEventListener('DOMContentLoaded', () => {
      const togglers = document.querySelectorAll(fullscreen.FULLSCREENTOGGLER);
      togglers?.forEach(toggler => toggler.addEventListener('click', e => {
        e.preventDefault();
        e.stopPropagation();
        this.toggleFullScreen();
      }));
    });
  }
  toggleFullScreen() {
    if (!document.fullscreenElement &&    // standard
      !document.mozFullScreenElement && !document.webkitFullscreenElement && !document.msFullscreenElement) {  // Vendor prefixes
      if (document.documentElement.requestFullscreen) {
        document.documentElement.requestFullscreen();
      } else if (document.documentElement.mozRequestFullScreen) {  // Firefox
        document.documentElement.mozRequestFullScreen();
      } else if (document.documentElement.webkitRequestFullscreen) {  // Chrome, Safari and Opera
        document.documentElement.webkitRequestFullscreen();
      } else if (document.documentElement.msRequestFullscreen) {  // IE/Edge
        document.documentElement.msRequestFullscreen();
      }
    } else {
      if (document.exitFullscreen) {
        document.exitFullscreen();
      } else if (document.mozCancelFullScreen) {  // Firefox
        document.mozCancelFullScreen();
      } else if (document.webkitExitFullscreen) {  // Chrome, Safari and Opera
        document.webkitExitFullscreen();
      } else if (document.msExitFullscreen) {  // IE/Edge
        document.msExitFullscreen();
      }
    }
  }
  onFullscreenChange() {
    const isFullscreen = document.fullscreenElement;
    var maxz = document.querySelectorAll(fullscreen.MAXIMIZEICON);
    maxz?.forEach(icon => icon.classList.toggle('d-none', isFullscreen));
    var minz = document.querySelectorAll(fullscreen.MINIMIZEICON);
    minz?.forEach(icon => icon.classList.toggle('d-none', !isFullscreen));
  }
}

class simplebar {
  constructor() {
    var style = document.createElement('style');
    style.innerHTML = '.simplebar-scrollbar::before { background-color: silver; }';
    document.documentElement.appendChild(style);
  }
}

class TooltipManager {
  constructor(container = document) {
    this.container = container;
    this.tooltipEl = null;
    this.hideTimer = null;
    this.init();
  }

  init() {
    this.createTooltipEl();

    // Attach hover listeners to all tooltip-enabled elements
    this.container.querySelectorAll('[data-bs-toggle="tooltip"]').forEach((el) => {
      el.addEventListener("mouseenter", (e) => this.handleEnter(e, el));
      el.addEventListener("mousemove", (e) => this.positionTooltip(e));
      el.addEventListener("mouseleave", () => this.handleLeave());
    });
  }

  createTooltipEl() {
    if (this.tooltipEl) return;
    this.tooltipEl = document.createElement("div");
    this.tooltipEl.className = "custom-tooltip";
    Object.assign(this.tooltipEl.style, {
      position: "fixed",
      zIndex: "1080",
      backgroundColor: "var(--bs-tertiary-bg)",
      color: "var(--bs-body-color)",
      padding: "4px 8px",
      borderRadius: "4px",
      fontSize: ".8rem",
      pointerEvents: "none",
      boxShadow: "0 2px 5px rgba(0,0,0,.25)",
      opacity: "0",
      transition: "opacity 0.05s linear",
      visibility: "hidden",
      maxWidth: "350px",
      whiteSpace: "nowrap",
      overflow: "hidden",
      textOverflow: "ellipsis"
    });
    document.body.appendChild(this.tooltipEl);
  }

  handleEnter(e, el) {
    clearTimeout(this.hideTimer);
    const text = el.getAttribute("title") || el.dataset.bsTitle || el.dataset.bsOriginalTitle || "";
    if (!text) return;

    // Remove native title tooltip
    el.dataset.bsOriginalTitle = text;
    el.removeAttribute("title");

    this.tooltipEl.textContent = text;
    this.positionTooltip(e);
    this.showTooltip();
  }

  handleLeave() {
    clearTimeout(this.hideTimer);
    this.hideTimer = setTimeout(() => this.hideTooltip(), 60); // slight delay avoids flicker
  }

  positionTooltip(e) {
    if (!this.tooltipEl) return;
    const offsetX = 4;
    const offsetY = 18;
    let x = e.clientX + offsetX;
    let y = e.clientY + offsetY;

    const rect = this.tooltipEl.getBoundingClientRect();
    const vw = window.innerWidth;
    const vh = window.innerHeight;

    // prevent off-screen
    if (x + rect.width > vw - 8) x = vw - rect.width - 8;
    if (y + rect.height > vh - 8) y = e.clientY - rect.height - offsetY;

    this.tooltipEl.style.left = `${x}px`;
    this.tooltipEl.style.top = `${y}px`;
  }

  showTooltip() {
    if (!this.tooltipEl) return;
    this.tooltipEl.style.visibility = "visible";
    this.tooltipEl.style.opacity = "1";
  }

  hideTooltip() {
    if (!this.tooltipEl) return;
    this.tooltipEl.style.opacity = "0";
    this.tooltipEl.style.visibility = "hidden";
  }

  // 🧹 Dispose safely
  dispose(container = document) {
    container.querySelectorAll('[data-bs-toggle="tooltip"]').forEach((el) => {
      el.removeEventListener("mouseenter", this.handleEnter);
      el.removeEventListener("mousemove", this.positionTooltip);
      el.removeEventListener("mouseleave", this.handleLeave);
    });
  }

  // ♻️ Quick refresh (destroy + rebuild)
  refresh(container = document) {
    this.dispose(container);
    new TooltipManager(container);
  }
}

class toggler {
  constructor() {
    const togglers = document.querySelectorAll("[data-toggle]");
    togglers.forEach(toggler => toggler.addEventListener("click", (e) => {
      e.preventDefault();
      e.stopPropagation();
      const toggleValue = e.currentTarget.getAttribute("data-toggle");
      if (toggleValue) {
        const targetElement = document.querySelector(e.currentTarget.getAttribute("data-target")) ?? document.body;
        targetElement.classList.toggle(toggleValue);
      }
    }));
  }
}

class BackdropKeydown {
  constructor() {
    document.addEventListener('keydown', e => {
      if (e.key && e.key.toLowerCase() == 'escape') {
        const backdrops = document.querySelectorAll('.backdrop');
        backdrops.forEach(backdrop => {
          if (window.getComputedStyle(backdrop).visibility == 'visible') {
            e.preventDefault();
            e.stopPropagation();
            backdrop.dispatchEvent(new Event('click'));
          }
        });
      }
    });
  }
}

class LeftbarCollapsedTogglerColor {
  constructor() {
    const updateLeftbarTogglerTheme = (target) => {
      const element = document.querySelector('[data-toggle="leftbar-collapsed"][data-bs-theme]');
      if (element) {
        element.style.transition = 'all .3s ease';
        if (document.body.classList.contains('leftbar-collapsed')) {
          element.setAttribute('data-bs-theme', bstheme.Theme);
        } else {
          element.setAttribute('data-bs-theme', 'dark');
        }
      }
    }
    const observer = new MutationObserver((mutations, observer) => {
      mutations.forEach(mutation => {
        if (mutation.type === 'attributes' && mutation.attributeName === 'class') {
          const cn = mutation.target.className;
          if ((mutation.oldValue?.includes('leftbar-collapsed') && !mutation.target.className.includes('leftbar-collapsed'))
            || ((!mutation.oldValue || !mutation.oldValue?.includes('leftbar-collapsed')) && mutation.target.className.includes('leftbar-collapsed'))) {
            updateLeftbarTogglerTheme(mutation.target);
          }
        }
      });
    });
    observer.observe(document.body, { attributes: true, attributeFilter: ['class'], attributeOldValue: true });
    document.addEventListener('bsthemechange', () => updateLeftbarTogglerTheme());
  }
}

class core {
  static get islocalhost() {
    if (window.location.hostname === 'localhost' ||
      window.location.hostname === '127.0.0.1' ||
      window.location.hostname === '::1') {
      return true;
    } else {
      return false;
    }
  }

  static swapimg() {
    const imgs = document.querySelectorAll("img[data-full]");
    imgs.forEach(img => {
      const fullUrl = img.dataset?.full;

      if (img.src == "") {
        img.src = fullUrl;
        return;
      }

      if (fullUrl) {
        const fullImg = new Image();
        fullImg.src = fullUrl;

        // Once the full url/cdn image loads → swap
        fullImg.onload = function () {
          img.src = fullUrl;

          // fade-in effect if you want..
          img.style.opacity = ".9";
          img.style.transition = ".3s ease-in-out";
        };
      }
    });
  }
  static imgerror(e, tempPhoto) {
    e.preventDefault();
    e.stopPropagation();
    //tempPhoto ??= `https://asset.yourvisioninfo.${(admin.islocalhost ? "host" : "com")}/cdn/admin/1.0.0/img/image.svg`
    if (tempPhoto) {
      e.currentTarget.src = tempPhoto;
      e.currentTarget.style.backgroundColor = "transparent";
      e.currentTarget.style.borderRadius = "var(--bs-border-radius)";
    } else {
      e.currentTarget.removeAttribute("src");
      e.currentTarget.style.display = "none";
    }
  }
  static alert(e, content, title, icon) {
    e.preventDefault();
    e.stopPropagation();
    $.alert({
      icon: icon,
      title: title,
      content: content,
      backgroundDismiss: true,
      theme: bstheme.Theme,
    });
  }
  static eyeslash() {
    //const selector1 = '.input-group:has(input[type=password])';
    //const selector2 = '.input-group:has(i.bi-eye-slash)';
    //const selectors = [...new Set([selector1, selector2])].join(', ');
    const selectors = '.input-group:has(i.bi-eye-slash)';

    document.querySelectorAll(selectors).forEach(group => {
      const inp = group.querySelector('input');
      const ico = group.querySelector('i');

      ico.addEventListener('click', e => {
        e.preventDefault();
        togglePassword(inp, ico);
      });

      updateIcon(inp, ico);

      function togglePassword(inp, ico) {
        if (!inp || !ico) return;
        inp.type = inp.type != 'password' ? 'password' : 'text';
        updateIcon(inp, ico);
      }

      function updateIcon(inp, ico) {
        if (!inp || !ico) return;
        const isPassword = inp.type == 'password';
        ico.classList.toggle('bi-eye-slash', isPassword);
        ico.classList.toggle('bi-eye', !isPassword);
      }
    });
  }
}

(() => document.addEventListener("DOMContentLoaded", () => {
  new toggler();
  new fullscreen();
  window.tooltipManager = new TooltipManager(); // ✅ Global singleton
  new simplebar();
  new BackdropKeydown();
  new LeftbarCollapsedTogglerColor();
  core.eyeslash();
  core.swapimg();
}))();

(() => new bstheme())();

//------------------------------------
// #region::js starting sequence
//
// (() => console.log())();
// (() => document.addEventListener('DOMContentLoaded', () => console.log()))();
// window.onload = () => console.log();
// $(() => console.log());
// $(document).ready(() => console.log());
// $(window).ready(() => console.log());
//
// #endregion::js starting sequence
//------------------------------------

//(function (global, factory) {
//  typeof exports === 'object' && typeof module !== 'undefined' ? factory(exports) :
//    typeof define === 'function' && define.amd ? define(['exports'], factory) :
//      (global = typeof globalThis !== 'undefined' ? globalThis : global || self, factory(global.adminlte = {}));
//})(this, (function (exports) {
//  'use strict';
//  const domContentLoadedCallbacks = [];
//  const onDOMContentLoaded = (callback) => {
//    if (document.readyState === 'loading') {
//      // add listener on the first call when the document is in loading state
//      if (!domContentLoadedCallbacks.length) {
//        document.addEventListener('DOMContentLoaded', () => {
//          for (const callback of domContentLoadedCallbacks) {
//            callback();
//          }
//        });
//      }
//      domContentLoadedCallbacks.push(callback);
//    }
//    else {
//      callback();
//    }
//  };
//  onDOMContentLoaded(() => new toggler());
//  onDOMContentLoaded(() => new fullscreen());
//  onDOMContentLoaded(() => new tooltip());
//  onDOMContentLoaded(() => new simplebar());
//  onDOMContentLoaded(() => new BackdropKeydown());
//  onDOMContentLoaded(() => new LeftbarCollapsedTogglerColor());
//}));
