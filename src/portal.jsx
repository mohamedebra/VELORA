import React from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import StaffPortal from './StaffPortal.jsx';

createRoot(document.getElementById('root')).render(<BrowserRouter><StaffPortal /></BrowserRouter>);
