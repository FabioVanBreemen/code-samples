# Unity game code
Dit zijn drie willekeurige scripts uit mijn project die een relatie met elkaar hebben.
Het volledige project bevat uiteraard veel meer scripts, maar hiermee is in het kort mijn manier van werken te zien. 

Ik documenteer mijn code, want op die manier blijft het overzichtelijk. Dit is vooral te zien in ItemManager.


## Over de scripts
ItemManager is een singleton die alle items uit het spel beheert, waaronder het huidige inventaris van de speler.

Inventory bevat alle logica van het inventaris menu. Hier staat code voor het openen en sluiten van het menu, maar ook het aanmaken en updaten van inventarisknoppen.

ItemButtonLogic zit op iedere item knop die in het inventaris menu is en bij een klik op de knop roept dit script een functie aan in ItemManager met een referentie naar de item info die gekoppeld is aan de knop.
