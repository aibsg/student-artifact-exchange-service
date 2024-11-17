import { MAIL_REGEX, USER_STATUS } from "./const.js";

const shareButton = document.querySelector('.note__share-button');
const shareOpen = document.querySelector('.share__open');
const cancelButton = document.querySelector('.share__cancel');
const deleteNoteButton = document.querySelector('.delete');
const approveOpen = document.querySelector('.approve__open');
const cancelDeleteButton = document.querySelector('.approve__cancel');
const inputEmail = document.querySelector('.input__email');
const shareOptions = document.querySelector('.share__options');
const saveShareButton = document.querySelector('.share__save');

//Открытие и закрытие окна с возможностью поделиться заметкой
if (shareButton) {
   shareButton.addEventListener('click', openShareModal)
}

if (cancelButton) {
   cancelButton.addEventListener('click', closeShareModal)
}

// Сохранение и отображение введенный данных в окне Поделиться
saveShareButton.addEventListener('click', saveShareModal);

function saveShareModal() {
   // Получаем значение введенного email
   const emailShareInput = inputEmail.value;
   const accessMode = shareOptions.value;

   // Проверяем, что email не пустой и соответствует формату
   if (!emailShareInput) {
      alert('Введите email');
      return;
   }
   if (!MAIL_REGEX.test(emailShareInput)) {
      alert('Введите корректный email');
      return;
   }

   // Формируем данные для отправки на сервер
   const requestData = {
      email: emailShareInput,
      accessMode: accessMode === 'reading' ? 'READ' : 'EDIT' // Пример значения режима доступа
   };

   // Отправляем данные на сервер
   fetch('http://localhost:5182/swagger/api/Document/ChangeAccess', {
      method: 'POST',
      headers: {
         'Content-Type': 'application/json'
      },
      body: JSON.stringify(requestData)
   })
   .then(response => {
      if (!response.ok) {
         throw new Error('Ошибка при отправке данных на сервер');
      }
      return response.json();
   })
   .then(data => {
      alert(`Доступ для ${emailShareInput} успешно обновлен на сервере.`);

      // Добавляем или обновляем UI (локальная обработка)
      const userList = document.querySelector('.users__with-access');
      const existingUser = Array.from(userList.querySelectorAll('.user__email'))
         .find(userEmail => userEmail.textContent === emailShareInput);

      if (existingUser) {
         const userModeText = existingUser.closest('.user__with-access__inner')
            .querySelector('.user__mode-text');
         userModeText.textContent = accessMode === 'reading' ? USER_STATUS.READING : USER_STATUS.EDITING;
      } else {
         const userWithAccessInner = document.createElement('div');
         userWithAccessInner.classList.add('user__with-access__inner');

         const userDiv = document.createElement('div');
         userDiv.classList.add('user__with-access');

         const userLogo = document.createElement('img');
         userLogo.classList.add('user__logo');
         userLogo.src = './images/user.svg';
         userLogo.alt = '';

         const userEmail = document.createElement('p');
         userEmail.classList.add('user__email');
         userEmail.textContent = emailShareInput;

         const userModeDiv = document.createElement('div');
         userModeDiv.classList.add('user__mode');

         const userModeText = document.createElement('p');
         userModeText.classList.add('user__mode-text');
         userModeText.textContent = accessMode === 'reading' ? USER_STATUS.READING : USER_STATUS.EDITING;

         userDiv.appendChild(userLogo);
         userDiv.appendChild(userEmail);

         userModeDiv.appendChild(userModeText);

         userWithAccessInner.appendChild(userDiv);
         userWithAccessInner.appendChild(userModeDiv);

         userList.appendChild(userWithAccessInner);

         inputEmail.value = '';
      }
   })
   .catch(error => {
      console.error(error);
      alert('Не удалось обновить доступ. Проверьте подключение к серверу.');
   });
}

//Окрытие и закрытие окна с подтверждением удаления заметки
if (deleteNoteButton) {
   deleteNoteButton.addEventListener('click', () => {
      approveOpen.classList.remove('hidden');
   });
}

if (cancelDeleteButton) {
   cancelDeleteButton.addEventListener('click', () => {
      approveOpen.classList.add('hidden');
   });
}

function openShareModal() {
   if (shareOpen) {
      shareOpen.classList.remove('hidden');
   }
}

function closeShareModal() {
   shareOpen.classList.add('hidden');
   inputEmail.value = '';
   shareOptions.selectedIndex = 0;
}

export {openShareModal, closeShareModal, saveShareModal};