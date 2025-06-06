window.localStorageHelper = {
    exportBoxToFile: function (storageKey, boxIdx, filename) {
        var box = null;
        for (let i = 0; i < localStorage.length; i++) {
            const key = localStorage.key(i);
            if (key.startsWith(storageKey)) {
                const pokemonStorageJson = localStorage.getItem(key);

                try {
                    const data = JSON.parse(pokemonStorageJson);
                    if (Array.isArray(data.boxes) && boxIdx >= 0 && boxIdx < data.boxes.length) {
                        box = data.boxes[boxIdx];
                    }
                } catch (e) {
                    console.error("Failed to parse JSON", e);
                }
            }
        }

        if (box == null) {
            console.error("Failed to export box.");
            return;
        }

        const blob = new Blob([JSON.stringify(box, null, 2)], { type: "application/json" });
        const url = URL.createObjectURL(blob);

        const a = document.createElement("a");
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    },

    importBoxFromFile: function (storageKey, fileInputId, dotNetHelper) {
        const input = document.getElementById(fileInputId);
        if (!input || !input.files.length) {
            return;
        }

        const file = input.files[0];
        const reader = new FileReader();

        reader.onload = () => {
            try {
                var pokemonStorage = null;
                for (let i = 0; i < localStorage.length; i++) {
                    const key = localStorage.key(i);
                    if (key.startsWith(storageKey)) {
                        const pokemonStorageJson = localStorage.getItem(key);

                        try {
                            pokemonStorage = JSON.parse(pokemonStorageJson);
                        } catch (e) {
                            console.error("Failed to parse the pokemon storage for input.", e);
                        }
                    }
                }

                if (pokemonStorage == null || !Array.isArray(pokemonStorage.boxes)) {
                    console.error("Failed to import box. Pokemon storage has not been created yet.");
                    throw new Error("Failed to import box. Pokemon storage has not been created yet.");
                }

                const boxJson = reader.result;
                dotNetHelper.invokeMethodAsync("ValidateBoxJson", boxJson)
                    .then(isValid => {
                        if (!isValid) {
                            console.error("Box JSON is invalid.");
                            dotNetHelper.invokeMethodAsync("OnUploadBoxFailure", "Box JSON is invalid.");
                            return;
                        }

                        const box = JSON.parse(boxJson);
                        pokemonStorage.boxes.push(box);
                        localStorage.setItem(storageKey, JSON.stringify(pokemonStorage));
                        dotNetHelper.invokeMethodAsync("OnUploadBoxSuccessAsync");
                    })
                    .catch((e) => {
                        dotNetHelper.invokeMethodAsync("OnUploadBoxFailure", e);
                    });
            } catch (e) {
                dotNetHelper.invokeMethodAsync("OnUploadBoxFailure", e);
            }
        };

        reader.readAsText(file);
    }
};