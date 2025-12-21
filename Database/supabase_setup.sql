-- Create macro_subscriptions table
CREATE TABLE IF NOT EXISTS macro_subscriptions (
    id SERIAL PRIMARY KEY,
    email TEXT NOT NULL UNIQUE,
    hardware_id TEXT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    subscription_start TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    subscription_type TEXT DEFAULT 'trial',
    expiry_date TIMESTAMP WITH TIME ZONE NULL,
    order_id TEXT NULL,
    last_check TIMESTAMP WITH TIME ZONE NULL,
    last_verified_timestamp TIMESTAMP WITH TIME ZONE NULL,
    last_verification_ip TEXT NULL,
    verification_count INTEGER DEFAULT 0,
    notes TEXT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_macro_subscriptions_email ON macro_subscriptions(email);
CREATE INDEX IF NOT EXISTS idx_macro_subscriptions_hardware_id ON macro_subscriptions(hardware_id);
CREATE INDEX IF NOT EXISTS idx_macro_subscriptions_order_id ON macro_subscriptions(order_id);
CREATE INDEX IF NOT EXISTS idx_macro_subscriptions_active ON macro_subscriptions(is_active);
CREATE INDEX IF NOT EXISTS idx_macro_subscriptions_last_verified ON macro_subscriptions(last_verified_timestamp);

-- Create function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create trigger to automatically update updated_at
DROP TRIGGER IF EXISTS update_macro_subscriptions_updated_at ON macro_subscriptions;
CREATE TRIGGER update_macro_subscriptions_updated_at
    BEFORE UPDATE ON macro_subscriptions
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Function to authenticate user with new macro_subscriptions table
CREATE OR REPLACE FUNCTION authenticate_user(user_email TEXT, user_hardware_id TEXT)
RETURNS JSON AS $$
DECLARE
    user_record macro_subscriptions%ROWTYPE;
    result JSON;
BEGIN
    -- Find user by email
    SELECT * INTO user_record FROM macro_subscriptions WHERE email = user_email;
    
    -- Check if user exists
    IF NOT FOUND THEN
        result := json_build_object(
            'success', false,
            'message', 'البريد الإلكتروني غير مسجل أو غير مفعل'
        );
        RETURN result;
    END IF;
    
    -- Check if user is active
    IF NOT user_record.is_active THEN
        result := json_build_object(
            'success', false,
            'message', 'الاشتراك غير مفعل'
        );
        RETURN result;
    END IF;
    
    -- Check expiry date
    IF user_record.expiry_date IS NOT NULL AND NOW() > user_record.expiry_date THEN
        result := json_build_object(
            'success', false,
            'message', 'انتهت صلاحية الاشتراك'
        );
        RETURN result;
    END IF;
    
    -- Check hardware ID
    IF user_record.hardware_id IS NULL THEN
        -- First time activation, save hardware ID and update verification timestamp
        UPDATE macro_subscriptions 
        SET hardware_id = user_hardware_id, 
            last_check = NOW(),
            last_verified_timestamp = NOW(),
            verification_count = verification_count + 1,
            updated_at = NOW()
        WHERE email = user_email;
        
        result := json_build_object(
            'success', true,
            'message', 'تم تفعيل الاشتراك بنجاح',
            'user', row_to_json(user_record)
        );
        RETURN result;
    ELSIF user_record.hardware_id = user_hardware_id THEN
        -- Hardware ID matches, update verification timestamp and allow access
        UPDATE macro_subscriptions 
        SET last_check = NOW(),
            last_verified_timestamp = NOW(),
            verification_count = verification_count + 1,
            updated_at = NOW()
        WHERE email = user_email;
        
        result := json_build_object(
            'success', true,
            'message', 'تم التحقق من الاشتراك بنجاح',
            'user', row_to_json(user_record)
        );
        RETURN result;
    ELSE
        -- Hardware ID doesn't match
        result := json_build_object(
            'success', false,
            'message', 'الاشتراك مرتبط بجهاز آخر. يرجى استخدام خيار إعادة الربط إذا كنت تريد ربط الاشتراك بهذا الجهاز.'
        );
        RETURN result;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Function to verify authentication (for periodic checks) - SERVER-SIDE ONLY
CREATE OR REPLACE FUNCTION verify_authentication(user_email TEXT, user_hardware_id TEXT, verification_ip TEXT)
RETURNS JSON AS $$
DECLARE
    user_record macro_fort_subscriptions%ROWTYPE;
    result JSON;
BEGIN
    -- Find user by email and hardware ID (exact match)
    SELECT * INTO user_record 
    FROM macro_fort_subscriptions 
    WHERE email = user_email AND hardware_id = user_hardware_id AND is_active = true;
    
    -- Check if user exists and is valid
    IF FOUND THEN
        -- Check expiry date
        IF user_record.expiry_date IS NOT NULL AND NOW() > user_record.expiry_date THEN
            result := json_build_object(
                'success', false,
                'message', 'انتهت صلاحية الاشتراك',
                'subscription_expired', true
            );
            RETURN result;
        END IF;
        
        -- Update verification timestamp and IP
        UPDATE macro_fort_subscriptions 
        SET last_verified_timestamp = NOW(),
            last_verification_ip = verification_ip,
            verification_count = verification_count + 1,
            updated_at = NOW()
        WHERE email = user_email AND hardware_id = user_hardware_id;
        
        result := json_build_object(
            'success', true,
            'message', 'تم التحقق من الاشتراك بنجاح',
            'subscription_type', user_record.subscription_type,
            'expiry_date', user_record.expiry_date,
            'is_active', user_record.is_active
        );
        RETURN result;
    END IF;
    
    -- If exact match not found, check if email exists with different hardware_id (for trial reactivation)
    SELECT * INTO user_record 
    FROM macro_fort_subscriptions 
    WHERE email = user_email AND is_active = true;
    
    IF FOUND THEN
        -- Check expiry date first
        IF user_record.expiry_date IS NOT NULL AND NOW() > user_record.expiry_date THEN
            result := json_build_object(
                'success', false,
                'message', 'انتهت صلاحية الاشتراك',
                'subscription_expired', true
            );
            RETURN result;
        END IF;
        
        -- If it's a trial subscription, allow hardware ID update (reinstall scenario)
        IF user_record.subscription_type = 'trial' THEN
            UPDATE macro_fort_subscriptions 
            SET hardware_id = user_hardware_id,
                last_verified_timestamp = NOW(),
                last_verification_ip = verification_ip,
                verification_count = verification_count + 1,
                updated_at = NOW()
            WHERE email = user_email;
            
            result := json_build_object(
                'success', true,
                'message', 'تم إعادة تفعيل الاشتراك على هذا الجهاز',
                'subscription_type', user_record.subscription_type,
                'expiry_date', user_record.expiry_date,
                'is_active', true,
                'reactivated', true
            );
            RETURN result;
        ELSE
            -- For paid subscriptions, don't allow automatic hardware ID change
            result := json_build_object(
                'success', false,
                'message', 'الاشتراك مرتبط بجهاز آخر. يرجى استخدام خيار إعادة الربط.'
            );
            RETURN result;
        END IF;
    END IF;
    
    -- Email not found at all
    result := json_build_object(
        'success', false,
        'message', 'البريد الإلكتروني غير مسجل'
    );
    
    RETURN result;
END;
$$ LANGUAGE plpgsql;

-- Function to reactivate subscription (reset hardware ID)
CREATE OR REPLACE FUNCTION reactivate_subscription(user_email TEXT, user_order_id TEXT)
RETURNS JSON AS $$
DECLARE
    user_record macro_subscriptions%ROWTYPE;
    result JSON;
BEGIN
    -- Find user by email and order ID
    SELECT * INTO user_record 
    FROM macro_subscriptions 
    WHERE email = user_email AND order_id = user_order_id AND is_active = true;
    
    -- Check if user exists with matching order ID
    IF FOUND THEN
        -- Reset hardware ID to allow new device binding
        UPDATE macro_subscriptions 
        SET hardware_id = NULL, 
            updated_at = NOW(),
            notes = COALESCE(notes, '') || ' | Device reset on ' || NOW()::TEXT
        WHERE email = user_email AND order_id = user_order_id;
        
        result := json_build_object(
            'success', true,
            'message', 'تم إعادة تعيين الاشتراك بنجاح. يمكنك الآن تفعيل الاشتراك على الجهاز الجديد.'
        );
    ELSE
        result := json_build_object(
            'success', false,
            'message', 'البيانات المدخلة غير صحيحة. يرجى التأكد من البريد الإلكتروني ورقم الطلب.'
        );
    END IF;
    
    RETURN result;
END;
$$ LANGUAGE plpgsql;

-- Insert sample data (for testing)
-- INSERT INTO macro_subscriptions (email, order_id, is_active, subscription_start) VALUES 
-- ('test@sr3h.com', 'SR3H001', true, NOW()),
-- ('user@sr3h.com', 'SR3H002', true, NOW());

-- Example: Add a real user
-- INSERT INTO macro_subscriptions (email, order_id, is_active, subscription_start, notes) VALUES 
-- ('customer@example.com', 'SR3H12345', true, NOW(), 'Test user for macro application');

-- Grant necessary permissions (adjust as needed for your Supabase setup)
-- GRANT SELECT, INSERT, UPDATE ON macro_subscriptions TO anon;
-- GRANT EXECUTE ON FUNCTION authenticate_user TO anon;
-- GRANT EXECUTE ON FUNCTION verify_authentication TO anon;
-- GRANT EXECUTE ON FUNCTION reactivate_subscription TO anon;