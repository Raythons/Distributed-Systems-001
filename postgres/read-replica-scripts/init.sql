CREATE TABLE IF NOT EXISTS products (
    id INT PRIMARY KEY,
    name TEXT NOT NULL,
    price NUMERIC NOT NULL,
    quantity INT NOT NULL,
    created_at BIGINT,
    updated_at BIGINT
);
