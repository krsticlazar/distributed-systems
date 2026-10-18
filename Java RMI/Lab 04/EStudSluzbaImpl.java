import java.rmi.*;
import java.rmi.server.*;
import java.util.*;

public class EStudSluzbaImpl extends UnicastRemoteObject implements EStudSluzba {
    private HashMap<String, Student> studenti;

    public EStudSluzbaImpl() throws RemoteException {
        super();
        studenti = new HashMap<String, Student>();
        dodajStudenta("9841");
        dodajStudenta("9842");
        dodajStudenta("9843");
        dodajStudenta("9844");
        dodajStudenta("9845");
        dodajStudenta("9846");
        dodajStudenta("9847");
        dodajStudenta("9848");
        dodajStudenta("9849");
        dodajStudenta("9850");
    }

    private void dodajStudenta(String brIndeksa) throws RemoteException {
        studenti.put(brIndeksa, new StudentImpl(brIndeksa));
    }

    public Student vratiStudenta(String brIndeksa) throws RemoteException {
        return studenti.get(brIndeksa);
    }
}
