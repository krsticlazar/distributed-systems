import java.rmi.*;

public interface Callback extends Remote {
    public void prostBrojGenerisan(int broj) throws RemoteException;
}
