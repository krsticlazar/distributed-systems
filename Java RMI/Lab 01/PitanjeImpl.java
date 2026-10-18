public class PitanjeImpl implements Pitanje {
    private static final long serialVersionUID = 1L;

    private final String tekst;
    private final String odgovorA;
    private final String odgovorB;
    private final String odgovorC;

    public PitanjeImpl(String tekst, String odgovorA, String odgovorB, String odgovorC) {
        this.tekst = tekst;
        this.odgovorA = odgovorA;
        this.odgovorB = odgovorB;
        this.odgovorC = odgovorC;
    }

    public String vratiTekst() {
        return tekst + System.lineSeparator()
                + "a) " + odgovorA + " b) " + odgovorB + " c) " + odgovorC;
    }
}
