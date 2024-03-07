import numpy as np
import matplotlib.pyplot as plt

# Données historiques des rendements du portefeuille
rendements_portefeuille = np.random.normal(0.0005, 0.02, 30)  # Remplacez cela par vos données réelles

# Montant initial du portefeuille
montant_initial = 10000

# Niveau de confiance
alpha = 0.05

# Calcul de la VaR historique
var_historique = -np.percentile(rendements_portefeuille * montant_initial, 100 * (1 - alpha))

# Affichage des résultats
print(f"VaR Historique (95% de confiance) sur 30 jours: {var_historique:.2f} euros")

# Création du graphique
plt.figure(figsize=(10, 6))
plt.plot(rendements_portefeuille * montant_initial, label='Rendements du Portefeuille')
plt.axhline(-var_historique, color='red', linestyle='dashed', linewidth=2, label=f'VaR Historique ({alpha * 100}% de confiance)')
plt.title('Rendements du Portefeuille et VaR Historique')
plt.xlabel('Jours')
plt.ylabel('Valeur')
plt.legend()
plt.show()
