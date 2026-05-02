
const modalElement = document.querySelector('#myModal');
if (modalElement) {
    const myModal = new bootstrap.Modal(modalElement);

    setTimeout(() => {
        myModal.hide();
    }, 1000);
}

//document.querySelector('.btn-close').addEventListener('click', () => {
//    myModal.hide();
//});