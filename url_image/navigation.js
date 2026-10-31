(() => {
  const root = document.getElementById("root");
  if (!root) return;

  const types = ["billboard", "poster"];
  const displayCounts = {};
  let selectedType;

  const findDisplayButton = (type) =>
    [...root.querySelectorAll('button[aria-pressed]')].find((button) =>
      new RegExp(`^${type}\\s+1(?:\\D|$)`, "i").test(button.textContent.trim()),
    );

  const updateNavigation = () => {
    const heading = [...root.querySelectorAll("h1")].find((item) =>
      item.textContent.trim().includes("Status Iklan VRChat"),
    );
    if (!heading) return;

    const titleBlock = heading.closest(".text-center");
    if (!titleBlock) return;

    let navigation = root.querySelector(".ad-quick-nav");
    if (!navigation) {
      navigation = document.createElement("nav");
      navigation.className = "ad-quick-nav";
      navigation.setAttribute("aria-label", "Navigasi kategori iklan");
      navigation.innerHTML = `
        <span class="ad-quick-nav__label">Jelajahi iklan</span>
        <div class="ad-quick-nav__tabs" role="group" aria-label="Pilih kategori iklan">
          ${types
            .map(
              (type) => `
                <button class="ad-quick-nav__tab" type="button" data-ad-type="${type}" aria-pressed="false">
                  <span>${type === "billboard" ? "Billboard" : "Poster"}</span>
                  <span class="ad-quick-nav__count" data-ad-count="${type}">0</span>
                </button>`,
            )
            .join("")}
        </div>`;

      navigation.addEventListener("click", (event) => {
        const tab = event.target.closest("[data-ad-type]");
        if (!tab) return;

        const type = tab.dataset.adType;
        let firstItem = findDisplayButton(type);
        if (!firstItem) {
          const groupLabel = `${type}s`;
          const groupToggle = [...root.querySelectorAll("aside button")].find(
            (button) => button.textContent.trim().toLowerCase() === groupLabel,
          );
          groupToggle?.click();
          requestAnimationFrame(() => findDisplayButton(type)?.click());
          return;
        }

        firstItem.click();
      });

      titleBlock.insertAdjacentElement("afterend", navigation);
    }

    const selectedItem = [...root.querySelectorAll('button[aria-pressed="true"]')].find(
      (button) => /^(billboard|poster)\s+\d+/i.test(button.textContent.trim()),
    );
    if (selectedItem) {
      selectedType = selectedItem.textContent.trim().split(/\s+/)[0].toLowerCase();
    }

    for (const type of types) {
      const count =
        displayCounts[type] ??
        [...root.querySelectorAll('button[aria-pressed]')].filter((button) =>
          new RegExp(`^${type}\\s+\\d+`, "i").test(button.textContent.trim()),
        ).length;
      const countLabel = navigation.querySelector(`[data-ad-count="${type}"]`);
      if (countLabel.textContent !== String(count)) {
        countLabel.textContent = count;
      }

      const tab = navigation.querySelector(`[data-ad-type="${type}"]`);
      const isSelected = String(selectedType === type);
      if (tab.getAttribute("aria-pressed") !== isSelected) {
        tab.setAttribute("aria-pressed", isSelected);
      }
    }
  };

  fetch("./data.json")
    .then((response) => {
      if (!response.ok) {
        throw new Error(`Gagal memuat data iklan: Status ${response.status}`);
      }
      return response.json();
    })
    .then((displays) => {
      for (const type of types) {
        displayCounts[type] = displays.filter((display) => display.type === type).length;
      }
      updateNavigation();
    })
    .catch((error) => console.error("Gagal memuat jumlah kategori iklan:", error));

  const observer = new MutationObserver(updateNavigation);
  observer.observe(root, {
    attributes: true,
    attributeFilter: ["aria-pressed"],
    childList: true,
    subtree: true,
  });
  updateNavigation();
})();
