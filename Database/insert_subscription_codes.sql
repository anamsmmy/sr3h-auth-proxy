-- Insert 5 subscription codes for each subscription type
-- Format: TYPE-XXXXXX-XXXXXX-XXXXXX
-- Generic codes with NULL email - redeemable by any user
-- expiry_date = NULL means code never expires (valid until used/redeemed)
-- subscription_type and duration_days determine what user gets when redeemed

-- MONTH subscriptions (30 days of service when redeemed)
INSERT INTO macro_fort_subscription_codes (code, email, subscription_type, duration_days, expiry_date)
VALUES
('MONTH-K7NM2P-X9QR4V-L8T1S5', NULL, 'month', 30, NULL),
('MONTH-R3F9B2-M7J4K1-W6G5H8', NULL, 'month', 30, NULL),
('MONTH-A5C2D8-E1N7P4-Q9M3R6', NULL, 'month', 30, NULL),
('MONTH-T4L9S2-V6W1X8-Y3Z7K5', NULL, 'month', 30, NULL),
('MONTH-B8H6J4-N2M5Q1-R7S9T3', NULL, 'month', 30, NULL);

-- SEMI subscriptions (180 days - 6 months of service when redeemed)
INSERT INTO macro_fort_subscription_codes (code, email, subscription_type, duration_days, expiry_date)
VALUES
('SEMI-P4G8K2-L9N3R1-S5T7V6', NULL, 'semi', 180, NULL),
('SEMI-X6W2M4-Y8B1C5-D9F3H7', NULL, 'semi', 180, NULL),
('SEMI-J2K7L4-Q6R9S1-T3V5W8', NULL, 'semi', 180, NULL),
('SEMI-E5M3N8-P7Q2R4-S6T9V1', NULL, 'semi', 180, NULL),
('SEMI-A9B6C2-D4F8G1-H5J7K3', NULL, 'semi', 180, NULL);

-- YEAR subscriptions (365 days of service when redeemed)
INSERT INTO macro_fort_subscription_codes (code, email, subscription_type, duration_days, expiry_date)
VALUES
('YEAR-M5N8P2-Q7R1S4-T9V3W6', NULL, 'year', 365, NULL),
('YEAR-X2Y6Z4-A8B1C5-D9F3H7', NULL, 'year', 365, NULL),
('YEAR-J4K7L2-M9N3P5-Q6R8S1', NULL, 'year', 365, NULL),
('YEAR-T5V9W2-X1Y7Z4-A3B8C6', NULL, 'year', 365, NULL),
('YEAR-D2F5G8-H1J4K6-L7M9N3', NULL, 'year', 365, NULL);

-- LIFETIME subscriptions (no expiry - NULL duration means forever)
INSERT INTO macro_fort_subscription_codes (code, email, subscription_type, duration_days, expiry_date)
VALUES
('LIFETIME-K3R7W2-M9N4P1-S8V5X6', NULL, 'lifetime', NULL, NULL),
('LIFETIME-A4B9C2-D7F1G5-H6J8K3', NULL, 'lifetime', NULL, NULL),
('LIFETIME-L2M6N8-Q5R9S1-T7V4W3', NULL, 'lifetime', NULL, NULL),
('LIFETIME-X1Y5Z9-A3B6C4-D8F2G7', NULL, 'lifetime', NULL, NULL),
('LIFETIME-H4J7K2-M1N8P6-Q3R5S9', NULL, 'lifetime', NULL, NULL);

-- Verify insertion
SELECT subscription_type, COUNT(*) as count FROM macro_fort_subscription_codes 
WHERE code LIKE 'MONTH-%' OR code LIKE 'SEMI-%' OR code LIKE 'YEAR-%' OR code LIKE 'LIFETIME-%'
GROUP BY subscription_type
ORDER BY subscription_type;
