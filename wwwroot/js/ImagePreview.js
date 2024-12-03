document.getElementById('imageFile').addEventListener('change', function (event) {
    const file = event.target.files[0]; // Obtiene el archivo seleccionado

    if (file) {
        const reader = new FileReader(); // Crea un lector de archivos

        reader.onload = function (e) {
            // Muestra la nueva imagen
            const previewImage = document.getElementById('previewImage');
            previewImage.src = e.target.result; // Establece la URL generada como fuente
            previewImage.style.display = 'block'; // Asegúrate de que sea visible

            // Opcional: Oculta la imagen actual
            const currentImage = document.getElementById('currentImage');
            if (currentImage) {
                currentImage.style.display = 'none';
            }
        };

        reader.readAsDataURL(file); // Lee el archivo como una URL de datos
    }
});
