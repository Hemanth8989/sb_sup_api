-- ============================================================
-- V020: Seed notification inbox data for dev testing
-- ============================================================

INSERT INTO notifications (id, tenant_id, user_id, type, title, body, entity_type, entity_id, link_url, is_read, read_at, created_at) VALUES

-- Supplier tenant (Marble Masters) — mix of read/unread
('10000001-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', NULL,
    'new_po', 'New purchase order received',
    'Countertop Kings placed a new purchase order for 12 slabs totalling $18,400.',
    'purchase_order', NULL, '/purchase-orders', FALSE, NULL, NOW() - INTERVAL '2 hours'),

('10000002-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', NULL,
    'connection_requested', 'New connection request',
    'Elite Surfaces & Stone has requested to connect with you as a preferred supplier.',
    'connection', NULL, '/connections', FALSE, NULL, NOW() - INTERVAL '5 hours'),

('10000003-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', NULL,
    'po_unacked_24h', 'Purchase order unacknowledged',
    'PO from Countertop Kings has been waiting for acknowledgement for more than 24 hours.',
    'purchase_order', NULL, '/purchase-orders', FALSE, NULL, NOW() - INTERVAL '26 hours'),

('10000004-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', NULL,
    'low_stock_warning', 'Low stock: Calacatta Gold',
    'You have only 2 available slabs of Calacatta Gold (Marble). Consider restocking soon.',
    'slab', NULL, '/inventory', FALSE, NULL, NOW() - INTERVAL '1 day'),

('10000005-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', NULL,
    'po_shipped', 'Purchase order shipped',
    'Your PO to Countertop Kings has been marked as shipped. Tracking: UPS 1Z999AA10123456784.',
    'purchase_order', NULL, '/purchase-orders', TRUE, NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),

('10000006-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', NULL,
    'connection_approved', 'Connection approved',
    'Elite Surfaces & Stone has approved your connection request. You can now view their catalog.',
    'connection', NULL, '/connections', TRUE, NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days'),

('10000007-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', NULL,
    'po_received', 'Purchase order received',
    'Countertop Kings confirmed receipt of their order. Payment will be processed within 5 business days.',
    'purchase_order', NULL, '/purchase-orders', TRUE, NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days'),

('10000008-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', NULL,
    'new_stock', 'New stock added to catalog',
    'Stone Source LLC added 24 new Quartzite slabs to their catalog. Check the supplier directory.',
    'slab', NULL, '/inventory', TRUE, NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days'),

('10000009-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', NULL,
    'price_changed', 'Price list updated',
    'Stone Source LLC updated their price list "Premium Stone 2024". 8 items have new pricing.',
    'price_list', NULL, '/price-lists', TRUE, NOW() - INTERVAL '6 days', NOW() - INTERVAL '6 days'),

('10000010-0000-0000-0000-000000000000', '11111111-1111-1111-1111-111111111111', NULL,
    'system', 'Platform maintenance scheduled',
    'StoneBridge will have a 30-minute maintenance window on Sunday 2026-06-22 at 2:00 AM ET.',
    NULL, NULL, NULL, TRUE, NOW() - INTERVAL '7 days', NOW() - INTERVAL '7 days'),

-- Fabricator tenant (Countertop Kings)
('20000001-0000-0000-0000-000000000000', '44444444-4444-4444-4444-444444444444', NULL,
    'po_acknowledged', 'Purchase order acknowledged',
    'Marble Masters Inc acknowledged your PO and will ship within 5 to 7 business days.',
    'purchase_order', NULL, '/purchase-orders', FALSE, NULL, NOW() - INTERVAL '3 hours'),

('20000002-0000-0000-0000-000000000000', '44444444-4444-4444-4444-444444444444', NULL,
    'delivery_confirmed', 'Delivery confirmed',
    'Your delivery from Marble Masters Inc was confirmed. 12 slabs received in good condition.',
    'purchase_order', NULL, '/purchase-orders', FALSE, NULL, NOW() - INTERVAL '8 hours');
