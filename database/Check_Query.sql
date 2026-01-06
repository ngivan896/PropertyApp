--check user --
SELECT 
    id,
    user_name,
    email,
    role,
    phone,
    created_at
FROM users
ORDER BY created_at DESC;

--check user based on email --
SELECT 
    id,
    user_name,
    email,
    role,
    phone,
    created_at
FROM users
WHERE email = 'admin@propertyapp.local';

-- check user based on role --
SELECT * FROM users WHERE role = 'admin';

SELECT * FROM users WHERE role = 'owner';

SELECT * FROM users WHERE role = 'worker';

