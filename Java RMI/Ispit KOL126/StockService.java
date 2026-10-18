import java.rmi.Remote;
import java.rmi.RemoteException;
import java.util.List;

public interface StockService extends Remote {
    List<Stock> getAllStocks() throws RemoteException;
    void changePrice(int id, double newPrice) throws RemoteException;
    void subscribe(Callback callback) throws RemoteException;
}
