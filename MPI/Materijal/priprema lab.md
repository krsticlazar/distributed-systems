Distribuirani sistemi

Priprema za treću laboratorijsku vežbu ( MPI IO)

Napisati MPI kod koji paralelno piše i čita binarnu datoteku prema sledećim koracima:

I)  Prvi  proces  upisuje  cele  brojeve  0-9  od  početka  datoteke,  drugi  proces  zapisuje  bojeve  10-19  sa
pomerajem 10 u datoteci i tako dalje. Koristiti pojedinačne pokazivače na datoteke.

II)  Ponovo  otvoriti  datoteku.  Svaki  proces  čita  podatke  upravo  napisane  koristeći  funkcije  sa
eksplicitnim pomerajem. Proveriti da li je čitanje pravilno izvršeno.

III) Svaki proces upisuje upravo pročitane podatke u novu datoteku, na sledeći način (pretpostavljajući
da postoje 4 procesa):

Poslednje upisivanje u datoteku učiniti što efikasnijim.

