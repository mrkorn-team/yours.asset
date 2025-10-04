/** Copyright © licensed - ยัวร์วิชั่นฯ (https://yourvisioninfo.com), all rights reserved. */
class gptmenu {
  constructor() {
    // Remember dropdown state
    const collapses = document.querySelectorAll(".gptmenu .collapse");

    collapses.forEach(collapse => {
      collapse.addEventListener("shown.bs.collapse", function () {
        localStorage.setItem("gptmenu-open", collapse.id);
      });
      collapse.addEventListener("hidden.bs.collapse", function () {
        if (localStorage.getItem("gptmenu-open") === collapse.id) {
          localStorage.removeItem("gptmenu-open");
        }
      });
    });

    // Re-open last active menu
    const lastOpen = localStorage.getItem("gptmenu-open");
    if (lastOpen) {
      const el = document.getElementById(lastOpen);
      if (el) {
        new bootstrap.Collapse(el, { toggle: true });
      }
    }

    // Highlight active link
    const navLinks = document.querySelectorAll(".gptmenu .nav-link");
    navLinks.forEach(item => {
      const menuLink = location.pathname + location.search;
      const href = item.getAttribute('href');
      const url = new URL(href, location.origin);
      const locHome = location.href.replace('index', '').replace(/\/+$/g, '') == location.origin;
      const urlHome = url.href.replace('index', '').replace(/\/+$/g, '') == url.origin;
      const active = (locHome && urlHome && item.id == 'home') || (!locHome && url.href == location.href);
      item.classList.toggle("active", active);

      // Expand parent menu if inside a collapsed group
      if (active) {
        const parentCollapse = item.closest(".collapse");
        if (parentCollapse) {
          new bootstrap.Collapse(parentCollapse, { toggle: true });
        }
      }
    });
  }
}

document.addEventListener("DOMContentLoaded", function () { new gptmenu(); });