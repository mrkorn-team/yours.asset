// wwwroot/js/sharedFiles.js
class SharedFiles {
  constructor() {
    this.init();
    this.enableSorting();     // initial table load
    this.refreshTooltips();   // init tooltips safely (scoped)
    this.initPrismTheme();    // handle prism theme switching
  }

  init() {
    console.log("SharedFiles browser loaded.");
    this.highlightActiveLinks();
    this.attachSearchClear();
    this.attachLiveSearch();
  }

  highlightActiveLinks() {
    const currentUrl = window.location.href;
    document.querySelectorAll(".list-group-item a").forEach(link => {
      const href = link.getAttribute("href");
      if (href && currentUrl.includes(href)) {
        link.classList.add("active");
      }
    });
  }

  attachSearchClear() {
    const searchInput = document.querySelector("input[name='search']");
    const clearBtn = document.querySelector("#searchClearBtn");
    if (!(searchInput && clearBtn)) return;

    const toggleClear = () => {
      clearBtn.style.display = searchInput.value.trim().length > 0 ? "inline-block" : "none";
    };

    toggleClear();

    searchInput.addEventListener("input", toggleClear);

    clearBtn.addEventListener("click", (e) => {
      e.preventDefault();
      this.clearSearch(searchInput, toggleClear);
    });

    document.addEventListener("keydown", (e) => {
      if (e.key === "Escape") {
        e.preventDefault();
        this.clearSearch(searchInput, toggleClear);
      }
    });

    searchInput.addEventListener("keydown", (e) => {
      if (e.key === "Enter") {
        e.preventDefault();
        this.updateSearchResults(searchInput.value);
      }
    });
  }

  clearSearch(searchInput, toggleFn) {
    searchInput.value = "";
    toggleFn();
    searchInput.focus();
    this.updateSearchResults(searchInput.value);
  }

  attachLiveSearch() {
    const searchInput = document.querySelector("input[name='search']");
    if (!searchInput) return;

    let debounceTimer;
    searchInput.addEventListener("input", () => {
      clearTimeout(debounceTimer);
      debounceTimer = setTimeout(() => {
        this.updateSearchResults(searchInput.value);
      }, 500);
    });
  }

  async updateSearchResults(query) {
    const resultsContainer = document.querySelector("#resultsContainer");
    if (!resultsContainer) return;

    const currentPath = document.querySelector("input[name='path']")?.value || "";
    const url = `/shared?path=${encodeURIComponent(currentPath)}&search=${encodeURIComponent(query)}`;

    // Update browser URL so refresh keeps search
    const newUrl = `${window.location.pathname}?path=${encodeURIComponent(currentPath)}&search=${encodeURIComponent(query)}`;
    window.history.replaceState({}, "", newUrl);

    try {
      resultsContainer.innerHTML = `
        <div class="text-center my-3">
          <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Loading...</span>
          </div>
        </div>`;

      const response = await fetch(url, { headers: { "X-Requested-With": "fetch" } });
      if (!response.ok) throw new Error(`HTTP ${response.status}`);

      const text = await response.text();
      const parser = new DOMParser();
      const doc = parser.parseFromString(text, "text/html");

      const table = doc.querySelector("#resultsTable");
      const alertBox = doc.querySelector(".alert");

      this.disposeTooltips(resultsContainer); // cleanup old tooltips

      if (table) {
        resultsContainer.innerHTML = table.outerHTML;
        this.enableSorting();
        this.refreshTooltips(resultsContainer);
      } else if (alertBox) {
        resultsContainer.innerHTML = alertBox.outerHTML;
        this.refreshTooltips(resultsContainer);
      } else {
        resultsContainer.innerHTML = `
          <div class="alert alert-secondary mb-0">
            <i class="bi bi-info-circle"></i> No results found.
          </div>`;
      }
    } catch (err) {
      console.error("Live search failed:", err);
      resultsContainer.innerHTML = `
        <div class="alert alert-danger mb-0">
          <i class="bi bi-exclamation-triangle"></i>
          Search failed. ${err?.message ? this.escapeHtml(err.message) : ""}
        </div>`;
    }
  }

  enableSorting() {
    const table = document.querySelector("#resultsTable");
    if (!table) return;

    const headers = table.querySelectorAll("th[data-sort]");
    const status = document.querySelector("#sortStatus");

    const updateStatus = (key, asc) => {
      if (!status) return;
      const colNames = { name: "Name", size: "Size", date: "Date" };
      const dirSymbol = asc ? "↑" : "↓";
      status.textContent = `Sorted by: ${colNames[key]} ${dirSymbol}`;
    };

    headers.forEach(header => {
      header.addEventListener("click", () => {
        const sortKey = header.getAttribute("data-sort");
        const tbody = table.querySelector("tbody");
        const rows = Array.from(tbody.querySelectorAll("tr"));

        let ascending;
        if ((sortKey === "size" || sortKey === "date") && !header.classList.contains("asc") && !header.classList.contains("desc")) {
          ascending = false; // first click → descending
        } else {
          ascending = !header.classList.contains("asc");
        }

        rows.sort((a, b) => {
          const aIsDir = a.dataset.isdir === "True";
          const bIsDir = b.dataset.isdir === "True";
          if (aIsDir && !bIsDir) return -1;
          if (!aIsDir && bIsDir) return 1;

          const aVal = this._getSortValue(a, sortKey);
          const bVal = this._getSortValue(b, sortKey);

          if (aVal < bVal) return ascending ? -1 : 1;
          if (aVal > bVal) return ascending ? 1 : -1;
          return 0;
        });

        headers.forEach(h => {
          h.classList.remove("asc", "desc");
          const icon = h.querySelector(".sort-icon");
          if (icon) icon.innerHTML = `<i class="bi bi-arrow-down-up"></i>`;
        });

        header.classList.add(ascending ? "asc" : "desc");
        const activeIcon = header.querySelector(".sort-icon");
        if (activeIcon) {
          activeIcon.innerHTML = ascending
            ? `<i class="bi bi-arrow-up"></i>`
            : `<i class="bi bi-arrow-down"></i>`;
        }

        tbody.replaceChildren(...rows);
        updateStatus(sortKey, ascending);
      });
    });

    const nameHeader = table.querySelector("th[data-sort='name']");
    if (nameHeader) {
      nameHeader.classList.add("asc");
      const icon = nameHeader.querySelector(".sort-icon");
      if (icon) icon.innerHTML = `<i class="bi bi-arrow-up"></i>`;
      updateStatus("name", true);
    }
  }

  _getSortValue(row, key) {
    switch (key) {
      case "name":
        return row.querySelector("td[data-name]").dataset.name.toLowerCase();
      case "size":
        return parseInt(row.querySelector("td[data-size]").dataset.size, 10);
      case "date":
        return parseInt(row.querySelector("td[data-date]").dataset.date, 10);
      default:
        return "";
    }
  }

  refreshTooltips(container = document) {
    container.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => {
      if (!bootstrap.Tooltip.getInstance(el)) new bootstrap.Tooltip(el);
    });
  }

  disposeTooltips(container = document) {
    container.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => {
      const instance = bootstrap.Tooltip.getInstance(el);
      if (instance) instance.dispose();
    });
  }

  escapeHtml(str) {
    return String(str)
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#039;");
  }

  // ✅ Prism theme switching
  initPrismTheme() {
    const setPrismTheme = (isDark) => {
      const prismTheme = document.getElementById("prismTheme");
      if (!prismTheme) return;

      const light = prismTheme.dataset.light;
      const dark = prismTheme.dataset.dark;

      prismTheme.href = isDark ? dark : light;
    };

    // Initial load
    const isDark = document.documentElement.getAttribute("data-bs-theme") === "dark";
    setPrismTheme(isDark);

    // Watch for Bootstrap theme toggle
    const observer = new MutationObserver(() => {
      const darkNow = document.documentElement.getAttribute("data-bs-theme") === "dark";
      setPrismTheme(darkNow);
    });
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ["data-bs-theme"] });
  }
}

document.addEventListener("DOMContentLoaded", () => new SharedFiles());
