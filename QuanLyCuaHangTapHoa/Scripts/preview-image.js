const fileInput = document.getElementById("file-input");
const thumbnailImg = document.getElementById("thumbnail-image");

fileInput.addEventListener("change", onFileChange);
function onFileChange(event) {
    const file = event.target.files[0];
    const imageUrl = URL.createObjectURL(file);

    thumbnailImg.src = imageUrl;
    thumbnailImg.width = 200;
    thumbnailImg.height = 200;
}
