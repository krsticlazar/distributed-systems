import java.io.Serializable;

public class Rezultat implements Serializable {
    private static final long serialVersionUID = 1L;

    private final int kandidatId;
    private final int brojGlasova;
    private final int mesto;

    public Rezultat(int kandidatId, int brojGlasova, int mesto) {
        this.kandidatId = kandidatId;
        this.brojGlasova = brojGlasova;
        this.mesto = mesto;
    }

    public int getKandidatId() {
        return kandidatId;
    }

    public int getBrojGlasova() {
        return brojGlasova;
    }

    public int getMesto() {
        return mesto;
    }

    public String toString() {
        return "Kandidat " + kandidatId + ": " + brojGlasova
                + " glasova, mesto " + mesto;
    }
}
