import java.rmi.*;

public interface EStudSluzba extends Remote {
    public Student vratiStudenta(String brIndeksa) throws RemoteException;
}
