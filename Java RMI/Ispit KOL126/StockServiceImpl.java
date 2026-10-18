import java.rmi.RemoteException;
import java.rmi.server.UnicastRemoteObject;
import java.util.ArrayList;
import java.util.List;

public class StockServiceImpl extends UnicastRemoteObject implements StockService {
    private final List<Stock> stocks = new ArrayList<>();
    private final List<Callback> callbacks = new ArrayList<>();

    public StockServiceImpl() throws RemoteException {
        super();
        stocks.add(new Stock(1, "Kompanija A", 100.0));
        stocks.add(new Stock(2, "Kompanija B", 200.0));
    }

    public synchronized List<Stock> getAllStocks() throws RemoteException {
        return new ArrayList<>(stocks);
    }

    public synchronized void subscribe(Callback callback) throws RemoteException {
        callbacks.add(callback);
    }

    public void changePrice(int id, double newPrice) throws RemoteException {
        List<Callback> pretplaceni = new ArrayList<>();

        synchronized (this) {
            for (Stock stock : stocks) {
                if (stock.getId() == id) {
                    stock.setPrice(newPrice);
                    pretplaceni = new ArrayList<>(callbacks);
                    break;
                }
            }
        }

        List<Callback> neaktivni = new ArrayList<>();

        for (Callback callback : pretplaceni) {
            try {
                callback.priceChanged(id, newPrice);
            } catch (RemoteException e) {
                neaktivni.add(callback);
            }
        }

        synchronized (this) {
            callbacks.removeAll(neaktivni);
        }
    }
}
