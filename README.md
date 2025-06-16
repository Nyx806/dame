# Jeu de Dames en Réseau

Un jeu de dames multijoueur en réseau utilisant WebSocket pour la communication en temps réel.

## Auteurs
- Antonin Merlo Meyffren
- Hamdaoui Mayssa

## Architecture

Le projet est divisé en trois parties principales :

### 1. CheckersGame.Shared
Contient les modèles et les messages partagés entre le client et le serveur :
- `Board.cs` : Représentation du plateau de jeu et logique de mouvement
- `Piece.cs` : Représentation des pièces (pions et dames)
- `Game.cs` : Logique de jeu et gestion des tours
- `Messages.cs` : Définition des messages échangés entre client et serveur

### 2. CheckersGame.Server
Serveur WebSocket qui gère :
- Les connexions des joueurs
- La création et la gestion des parties
- La validation des mouvements
- La synchronisation de l'état du jeu

### 3. CheckersGame.Client
Interface graphique WPF qui permet :
- La connexion au serveur
- L'affichage du plateau de jeu
- L'interaction avec les pièces
- La visualisation des mouvements en temps réel

## Représentation des Données Échangées

Les messages sont échangés en JSON via WebSocket :

1. `JoinGameMessage` : Demande de rejoindre une partie
2. `GameStartedMessage` : Notification du début d'une partie
3. `MakeMoveMessage` : Demande de mouvement
4. `MoveResultMessage` : Résultat d'un mouvement
5. `GameStateMessage` : État actuel du jeu

## Politique de Gestion des Conflits

1. **Synchronisation des Tours** :
   - Le serveur maintient l'état du tour actuel
   - Les mouvements sont rejetés si ce n'est pas le tour du joueur

2. **Validation des Mouvements** :
   - Le serveur valide chaque mouvement
   - Les mouvements invalides sont rejetés avec un message d'erreur

3. **Gestion des Déconnexions** :
   - Les joueurs déconnectés sont automatiquement retirés de la partie
   - La partie est marquée comme terminée si un joueur se déconnecte

## Synchronisation de l'État de Jeu

1. **Mise à jour en Temps Réel** :
   - Le serveur envoie l'état complet du jeu après chaque mouvement
   - Les clients mettent à jour leur interface en conséquence

2. **Cohérence des Données** :
   - Le serveur est la source unique de vérité
   - Les clients reflètent l'état du serveur

## Installation et Exécution

### Prérequis
- .NET 8.0 SDK
- Visual Studio 2022 ou supérieur

### Compilation
```bash
dotnet build
```

### Lancement
1. Démarrer le serveur :
```bash
cd CheckersGame.Server
dotnet run
```

2. Lancer les clients (dans des terminaux séparés) :
```bash
cd CheckersGame.Client
dotnet run
```

## Cas d'Utilisation

### Scénario de Base
1. Le joueur 1 lance le client et rejoint une partie
2. Le joueur 2 lance le client et rejoint la même partie
3. La partie commence automatiquement
4. Les joueurs jouent à tour de rôle
5. Le premier joueur à capturer toutes les pièces de l'adversaire gagne

### Scénario de Déconnexion
1. Un joueur se déconnecte pendant la partie
2. Le serveur détecte la déconnexion
3. La partie est marquée comme terminée
4. L'autre joueur est notifié

## Structure du Projet
```
CheckersGame/
├── CheckersGame.Client/        # Interface utilisateur WPF
├── CheckersGame.Server/        # Serveur WebSocket
└── CheckersGame.Shared/        # Modèles et messages partagés
```

## Technologies Utilisées
- C# / .NET 8.0
- WPF pour l'interface client
- WebSocket pour la communication en temps réel
- JSON pour la sérialisation des messages 