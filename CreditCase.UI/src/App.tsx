import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import { Sidebar } from './components/layout/Sidebar';
import { Dashboard } from './pages/Dashboard';
import { Customers } from './pages/Customers';
import { CustomerDetail } from './pages/CustomerDetail';
import { Loans } from './pages/Loans';
import { LoanDetail } from './pages/LoanDetail';
import { Payments } from './pages/Payments';

export default function App() {
  return (
    <BrowserRouter>
      <div className="flex min-h-screen bg-[#F1F5F9]">
        <Sidebar />
        <main className="flex-1 overflow-auto">
          <Routes>
            <Route path="/"               element={<Dashboard />} />
            <Route path="/customers"      element={<Customers />} />
            <Route path="/customers/:id"  element={<CustomerDetail />} />
            <Route path="/loans"          element={<Loans />} />
            <Route path="/loans/:id"      element={<LoanDetail />} />
            <Route path="/payments"       element={<Payments />} />
          </Routes>
        </main>
      </div>
      <Toaster position="top-right" toastOptions={{ duration: 3000 }} />
    </BrowserRouter>
  );
}
