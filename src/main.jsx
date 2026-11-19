import React from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import './style.css';
import './products.css';
import { ProductsScreen, ProductDetailScreen } from './ProductScreen.jsx';
import { CollectionScreen, CustomMirrorScreen, ContactScreen } from './VeloraPages.jsx';
import StaffPortal from './StaffPortal.jsx';

function App() {
  return <Routes>
    <Route path="/" element={<ProductsScreen />} />
    <Route path="/shop" element={<CollectionScreen />} />
    <Route path="/mirror/:id" element={<ProductDetailScreen />} />
    <Route path="/custom" element={<CustomMirrorScreen />} />
    <Route path="/contact" element={<ContactScreen />} />
    <Route path="/dashboard" element={<StaffPortal />} />
    <Route path="/dashbord" element={<StaffPortal />} />
    <Route path="*" element={<ProductsScreen />} />
  </Routes>;
}

createRoot(document.getElementById('root')).render(<BrowserRouter><App /></BrowserRouter>);
