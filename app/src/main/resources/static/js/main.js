'use strict';

// Éléments du DOM
// Supprimer les éléments liés à la page de connexion manuelle
// const usernamePage = document.querySelector('#username-page');
// const usernameInput = document.querySelector('#username');
// const connectButton = document.querySelector('#connect-button');

const authContainer = document.querySelector('#auth-container');
const loginForm = document.querySelector('#login-form');
const registerForm = document.querySelector('#register-form');
const switchToRegisterLink = document.querySelector('#switch-to-register');
const switchToLoginLink = document.querySelector('#switch-to-login');
const authTitle = document.querySelector('#auth-title');
const authError = document.querySelector('#auth-error');

const mainContainer = document.querySelector('#main-container'); // Conteneur principal du chat
const chatPage = document.querySelector('#chat-page');
const disconnectButton = document.querySelector('#disconnect-button');
const sendButton = document.querySelector('#send-button');
const messageInput = document.querySelector('#message');
// const recipientInput = document.querySelector('#recipient'); // Remplacé par une liste
const messageArea = document.querySelector('#message-area');
const userList = document.querySelector('#user-list'); // Nouvel élément pour la liste des utilisateurs
const currentConversationTitle = document.querySelector('#current-conversation-title');

// Variables globales
let stompClient = null;
let currentUsername = null; // Sera défini après validation du token
let selectedUser = null; // Utilisateur sélectionné pour la conversation privée

// Couleurs pour les avatars (simpliste)
const colors = [
    '#2196F3', '#32c787', '#00BCD4', '#ff5652',
    '#ffc107', '#ff85af', '#FF9800', '#39bbb0'
];

// --- Authentification et Initialisation ---

async function handleLogin(event) {
    event.preventDefault();
    const username = document.querySelector('#login-username').value.trim();
    const password = document.querySelector('#login-password').value.trim();
    hideAuthError();

    if (!username || !password) {
        showAuthError("Veuillez remplir tous les champs.");
        return;
    }

    try {
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.message || "Identifiants invalides");
        }

        console.log("Login réussi:", data);
        // Stocker le token et le nom d'utilisateur
        localStorage.setItem('jwtToken', data.token);
        localStorage.setItem('username', data.username);

        // Démarrer le chat
        initializeChat();

    } catch (error) {
        console.error("Erreur de connexion:", error);
        showAuthError(error.message || "Erreur lors de la connexion.");
    }
}

async function handleRegister(event) {
    event.preventDefault();
    const username = document.querySelector('#register-username').value.trim();
    const email = document.querySelector('#register-email').value.trim();
    const password = document.querySelector('#register-password').value.trim();
    hideAuthError();

    if (!username || !email || !password) {
        showAuthError("Veuillez remplir tous les champs.");
        return;
    }

    try {
        const response = await fetch('/api/auth/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, email, password })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.message || "Erreur lors de l'inscription");
        }

        console.log("Inscription réussie:", data);
        // Stocker le token et le nom d'utilisateur
        localStorage.setItem('jwtToken', data.user.username); // Le nom est dans data.user
        localStorage.setItem('username', data.user.username);

        // Démarrer le chat
        initializeChat();

    } catch (error) {
        console.error("Erreur d'inscription:", error);
        showAuthError(error.message || "Erreur lors de l'inscription.");
    }
}

function showAuthError(message) {
    authError.textContent = message;
    authError.classList.remove('hidden');
}

function hideAuthError() {
    authError.classList.add('hidden');
}

function switchToRegisterView() {
    loginForm.classList.add('hidden');
    registerForm.classList.remove('hidden');
    switchToRegisterLink.classList.add('hidden');
    switchToLoginLink.classList.remove('hidden');
    authTitle.textContent = "Inscription";
    hideAuthError();
}

function switchToLoginView() {
    loginForm.classList.remove('hidden');
    registerForm.classList.add('hidden');
    switchToRegisterLink.classList.remove('hidden');
    switchToLoginLink.classList.add('hidden');
    authTitle.textContent = "Connexion";
    hideAuthError();
}

function initializeChat() {
    const token = localStorage.getItem('jwtToken');
    const storedUsername = localStorage.getItem('username');

    if (token && storedUsername) {
        currentUsername = storedUsername;
        // Cacher l'authentification et afficher le chat
        authContainer.classList.add('hidden');
        mainContainer.classList.remove('hidden');
        connectWebSocket(token);
    } else {
        // Si pas de token, s'assurer que l'écran de login est visible
        authContainer.classList.remove('hidden');
        mainContainer.classList.add('hidden');
        console.log("Pas de session active, affichage de l'écran de connexion.");
    }
}

// --- Connexion WebSocket avec JWT ---
function connectWebSocket(token) {
    console.log("Tentative de connexion WebSocket avec token...");

    const socket = new SockJS('/ws');
    stompClient = Stomp.over(socket);

    // Headers pour la connexion STOMP, incluant le token JWT
    const headers = {
        'Authorization': 'Bearer ' + token
    };

    stompClient.connect(headers, onConnected, onError);
}

function onConnected() {
    console.log('Connecté au serveur WebSocket via STOMP');

    // S'abonne au topic public
    stompClient.subscribe('/topic/public', onMessageReceived);

    // S'abonne à la file d'attente privée de l'utilisateur
    stompClient.subscribe('/user/' + currentUsername + '/queue/messages', onMessageReceived);

    // Récupérer la liste des utilisateurs
    fetchUsers();

    // Charger l'historique public par défaut (ou une conversation spécifique si nécessaire)
    selectConversation('public'); // Par défaut, afficher le chat public
}

function onError(error) {
    console.error('Erreur de connexion WebSocket:', error);
    alert('Impossible de se connecter au serveur de chat. Vérifiez votre connexion ou réessayez plus tard.');
    // Nettoyer et retourner à l'écran de login
    disconnectCleanup();
}

// --- Récupération et affichage des utilisateurs ---
async function fetchUsers() {
    const token = localStorage.getItem('jwtToken');
    if (!token) return;

    try {
        const response = await fetch('/api/users', {
            headers: { 'Authorization': 'Bearer ' + token }
        });
        if (!response.ok) throw new Error(`Erreur HTTP: ${response.status}`);
        const users = await response.json();

        userList.innerHTML = ''; // Vide la liste existante
        // Ajoute l'option pour le chat public
        const publicOption = document.createElement('li');
        publicOption.textContent = "Chat Public";
        publicOption.classList.add('list-group-item', 'list-group-item-action', 'active'); // Actif par défaut
        publicOption.dataset.username = 'public';
        publicOption.onclick = () => selectConversation('public');
        userList.appendChild(publicOption);

        // Ajoute les autres utilisateurs
        users.forEach(user => {
            const userElement = document.createElement('li');
            userElement.textContent = user.username;
            userElement.classList.add('list-group-item', 'list-group-item-action');
            userElement.dataset.username = user.username;
            userElement.onclick = () => selectConversation(user.username);
            userList.appendChild(userElement);
        });
    } catch (error) {
        console.error("Impossible de récupérer la liste des utilisateurs:", error);
    }
}

// --- Sélection de conversation et chargement de l'historique ---
function selectConversation(username) {
    console.log("Sélection de la conversation avec:", username);
    selectedUser = (username === 'public') ? null : username; // null pour public

    // Met à jour le titre
    currentConversationTitle.textContent = selectedUser ? `Conversation avec ${selectedUser}` : "Chat Public";

    // Met à jour l'UI pour montrer l'utilisateur actif
    document.querySelectorAll('#user-list li').forEach(li => {
        li.classList.remove('active', 'font-weight-bold', 'text-primary'); // Nettoie aussi le highlight
        if (li.dataset.username === username) {
            li.classList.add('active');
        }
    });

    // Charge l'historique correspondant
    fetchHistory(selectedUser);
}

async function fetchHistory(targetUser = null) { // targetUser est null pour public
    messageArea.innerHTML = ''; // Vide la zone
    const token = localStorage.getItem('jwtToken');
    if (!token) return;

    let url;
    if (targetUser) {
        url = `/api/messages/${targetUser}`; // Utilise le nouvel endpoint
    } else {
        // Ici, on pourrait charger l'historique public si un endpoint existait
        console.log("Chargement de l'historique public...");
        // url = '/api/messages/public'; 
        // Pour l'instant, on affiche juste les messages reçus sur /topic/public en temps réel
         currentConversationTitle.textContent = "Chat Public"; // S'assure que le titre est correct
        return; // Ne charge pas d'historique pour le public via API
    }

    try {
        const response = await fetch(url, {
            headers: { 'Authorization': 'Bearer ' + token }
        });
        if (!response.ok) throw new Error(`Erreur HTTP: ${response.status}`);
        const historyMessages = await response.json();

        console.log("Historique reçu:", historyMessages);
        historyMessages.forEach(displayMessage);
        messageArea.scrollTop = messageArea.scrollHeight;

    } catch (error) {
        console.error(`Impossible de récupérer l'historique pour ${targetUser}:`, error);
        const errorElement = document.createElement('p');
        errorElement.textContent = "Impossible de charger l'historique des messages.";
        errorElement.style.color = 'red';
        errorElement.style.textAlign = 'center';
        messageArea.appendChild(errorElement);
    }
}

// --- Envoi de message ---
function sendMessage(event) {
    event.preventDefault();
    const messageContent = messageInput.value.trim();

    if (messageContent && stompClient) {
        const chatMessage = {
            content: messageContent,
            // Le destinataire est l'utilisateur sélectionné (null si chat public)
            recipientUsername: selectedUser 
        };

        stompClient.send("/app/chat.sendMessage", {}, JSON.stringify(chatMessage));
        messageInput.value = '';
    } else if (!stompClient) {
        console.warn("Client STOMP non connecté.");
    }
}

// --- Réception et Affichage de message (displayMessage reste similaire) ---
function onMessageReceived(payload) {
    console.log("[onMessageReceived] Payload reçu:", payload);
    let message;
    try {
        message = JSON.parse(payload.body);
        console.log("[onMessageReceived] Message parsé:", message);
    } catch (e) {
        console.error("[onMessageReceived] Erreur lors du parsing JSON:", e, payload.body);
        return;
    }

    const isPublic = message.recipientUsername === null;
    const isPrivateForMe = message.recipientUsername === currentUsername || message.senderUsername === currentUsername;
    const involvesSelectedUser = selectedUser && (message.senderUsername === selectedUser || message.recipientUsername === selectedUser);

    console.log(`[onMessageReceived] Détails: isPublic=${isPublic}, isPrivateForMe=${isPrivateForMe}, involvesSelectedUser=${involvesSelectedUser}, selectedUser=${selectedUser}, currentUsername=${currentUsername}`);

    // Simplification temporaire pour le debug: Afficher tous les messages reçus
    // console.log("[onMessageReceived] DEBUG: Affichage forcé du message.");
    // displayMessage(message); 
    // Fin de la simplification

    // Logique originale:
    if ((isPublic && selectedUser === null) || (selectedUser && isPrivateForMe && involvesSelectedUser) ) {
         console.log("[onMessageReceived] Affichage du message car il correspond à la conversation actuelle.");
         displayMessage(message);
    } else {
        console.log("[onMessageReceived] Message ignoré (pas pour la conversation actuelle)");
        // Notification même si non affiché
        if (message.senderUsername !== currentUsername && (document.hidden || selectedUser !== message.senderUsername) ) { 
             console.log("[onMessageReceived] Notification pour message non affiché.");
             notifyUser(`Nouveau message de ${message.senderUsername}`);
             highlightUserWithNewMessage(message.senderUsername);
        }
    }
}

function displayMessage(message) {
    const messageElement = document.createElement('div');
    messageElement.classList.add('message');
    if (message.senderUsername === currentUsername) {
        messageElement.classList.add('sent');
    } else {
        messageElement.classList.add('received');
    }
    const avatarElement = document.createElement('span');
    const avatarText = document.createTextNode(message.senderUsername[0]);
    avatarElement.appendChild(avatarText);
    avatarElement.style['background-color'] = getAvatarColor(message.senderUsername);
    avatarElement.style['color'] = '#fff';
    avatarElement.style['padding'] = '5px 8px';
    avatarElement.style['border-radius'] = '50%';
    avatarElement.style['margin-right'] = '8px';
    messageElement.appendChild(avatarElement);

    const infoElement = document.createElement('div');
    infoElement.classList.add('sender');
    infoElement.textContent = message.senderUsername;
    messageElement.appendChild(infoElement);

    const textElement = document.createElement('p');
    textElement.textContent = message.content;
    messageElement.appendChild(textElement);

    const timeElement = document.createElement('span');
    timeElement.classList.add('timestamp');
    const messageTime = new Date(message.timestamp);
    timeElement.textContent = messageTime.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    messageElement.appendChild(timeElement);

    messageArea.appendChild(messageElement);
    messageArea.scrollTop = messageArea.scrollHeight;
}

function highlightUserWithNewMessage(username) {
    document.querySelectorAll('#user-list li').forEach(li => {
        if (li.dataset.username === username && !li.classList.contains('active')) {
            li.classList.add('font-weight-bold', 'text-primary'); // Exemple de mise en évidence
        }
    });
}

// --- Déconnexion ---
function disconnect(event) {
    if(event) event.preventDefault(); // Peut être appelé sans événement
    if (stompClient !== null) {
        try {
             stompClient.disconnect(() => { console.log("STOMP Déconnecté"); });
        } catch (e) { console.warn("Erreur lors de la déconnexion STOMP:", e); }
        stompClient = null;
    }
    disconnectCleanup();
}

// Fonction séparée pour le nettoyage UI/localStorage
function disconnectCleanup() {
    localStorage.removeItem('jwtToken');
    localStorage.removeItem('username');
    currentUsername = null;
    selectedUser = null;
    mainContainer.classList.add('hidden');
    authContainer.classList.remove('hidden');
    switchToLoginView(); // S'assure que le formulaire de login est visible
    userList.innerHTML = '<li class="list-group-item">Déconnecté</li>'; // Vide la liste des users
    messageArea.innerHTML = ''; // Vide les messages
    console.log("Interface et session nettoyées.");
}

// --- Fonctions utilitaires (getAvatarColor, notifyUser) ---
function getAvatarColor(messageSender) {
    let hash = 0;
    for (let i = 0; i < messageSender.length; i++) {
        hash = 31 * hash + messageSender.charCodeAt(i);
    }
    const index = Math.abs(hash % colors.length);
    return colors[index];
}
function notifyUser(message) {
    if (!("Notification" in window)) { return; }
    if (Notification.permission === "granted") {
        new Notification(message);
    } else if (Notification.permission !== "denied") {
        Notification.requestPermission().then(permission => {
            if (permission === "granted") { new Notification(message); }
        });
    }
}

// --- Initialisation et Écouteurs d'événements ---
document.addEventListener('DOMContentLoaded', () => {
    // Essayer d'initialiser le chat si un token existe
    initializeChat(); 

    // Écouteurs pour les formulaires d'authentification
    if(loginForm) loginForm.addEventListener('submit', handleLogin);
    if(registerForm) registerForm.addEventListener('submit', handleRegister);
    if(switchToRegisterLink) switchToRegisterLink.addEventListener('click', switchToRegisterView);
    if(switchToLoginLink) switchToLoginLink.addEventListener('click', switchToLoginView);

    // Écouteurs pour le chat (seront actifs seulement si le chat est visible)
    if(disconnectButton) disconnectButton.addEventListener('click', disconnect);
    if(sendButton) sendButton.addEventListener('click', sendMessage);
    // Gérer l'envoi avec la touche Entrée
    if(messageInput) {
        messageInput.addEventListener('keypress', function(event) {
            if (event.key === 'Enter') {
                sendMessage(event);
            }
        });
    }
});

// Cache la page de chat initialement (si non géré par la logique d'authentification)
// chatPage.classList.add('hidden');

// --- Fonction pour afficher un message dans le DOM ---
function displayMessage(message) {
    const messageElement = document.createElement('div');
    messageElement.classList.add('message');

    // Détermine si le message est envoyé ou reçu par l'utilisateur actuel
    if (message.senderUsername === currentUsername) {
        messageElement.classList.add('sent');
    } else {
        messageElement.classList.add('received');
    }

    // Avatar simplifié (initiale avec couleur)
    const avatarElement = document.createElement('span');
    const avatarText = document.createTextNode(message.senderUsername[0]);
    avatarElement.appendChild(avatarText);
    avatarElement.style['background-color'] = getAvatarColor(message.senderUsername);
    avatarElement.style['color'] = '#fff';
    avatarElement.style['padding'] = '5px 8px';
    avatarElement.style['border-radius'] = '50%';
    avatarElement.style['margin-right'] = '8px';
    // messageElement.appendChild(avatarElement); // Décommentez pour afficher l'avatar

    // Informations du message
    const infoElement = document.createElement('div');
    infoElement.classList.add('sender');
    infoElement.textContent = message.senderUsername + (message.recipientUsername ? ' à ' + message.recipientUsername : '');
    messageElement.appendChild(infoElement);

    // Contenu du message
    const textElement = document.createElement('p');
    textElement.textContent = message.content;
    messageElement.appendChild(textElement);

    // Timestamp
    const timeElement = document.createElement('span');
    timeElement.classList.add('timestamp');
    // Formater le timestamp (simpliste)
    const messageTime = new Date(message.timestamp);
    timeElement.textContent = messageTime.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    messageElement.appendChild(timeElement);

    // Ajoute le message à la zone de chat
    messageArea.appendChild(messageElement);
    // Fait défiler vers le bas pour voir le dernier message
    messageArea.scrollTop = messageArea.scrollHeight;

    // Notification (très basique)
    if (document.hidden && message.senderUsername !== currentUsername) { // Si l'onglet n'est pas actif
        notifyUser("Nouveau message de " + message.senderUsername);
    }
}

// --- Fonction pour afficher un message dans le DOM ---
function displayMessage(message) {
    const messageElement = document.createElement('div');
    messageElement.classList.add('message');

    // Détermine si le message est envoyé ou reçu par l'utilisateur actuel
    if (message.senderUsername === currentUsername) {
        messageElement.classList.add('sent');
    } else {
        messageElement.classList.add('received');
    }

    // Avatar simplifié (initiale avec couleur)
    const avatarElement = document.createElement('span');
    const avatarText = document.createTextNode(message.senderUsername[0]);
    avatarElement.appendChild(avatarText);
    avatarElement.style['background-color'] = getAvatarColor(message.senderUsername);
    avatarElement.style['color'] = '#fff';
    avatarElement.style['padding'] = '5px 8px';
    avatarElement.style['border-radius'] = '50%';
    avatarElement.style['margin-right'] = '8px';
    // messageElement.appendChild(avatarElement); // Décommentez pour afficher l'avatar

    // Informations du message
    const infoElement = document.createElement('div');
    infoElement.classList.add('sender');
    infoElement.textContent = message.senderUsername + (message.recipientUsername ? ' à ' + message.recipientUsername : '');
    messageElement.appendChild(infoElement);

    // Contenu du message
    const textElement = document.createElement('p');
    textElement.textContent = message.content;
    messageElement.appendChild(textElement);

    // Timestamp
    const timeElement = document.createElement('span');
    timeElement.classList.add('timestamp');
    // Formater le timestamp (simpliste)
    const messageTime = new Date(message.timestamp);
    timeElement.textContent = messageTime.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    messageElement.appendChild(timeElement);

    // Ajoute le message à la zone de chat
    messageArea.appendChild(messageElement);
    // Fait défiler vers le bas pour voir le dernier message
    messageArea.scrollTop = messageArea.scrollHeight;

    // Notification (très basique)
    if (document.hidden && message.senderUsername !== currentUsername) { // Si l'onglet n'est pas actif
        notifyUser("Nouveau message de " + message.senderUsername);
    }
} 