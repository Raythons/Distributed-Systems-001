-- Create the initial schema for our CQRS system
CREATE TABLE IF NOT EXISTS products (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    price DECIMAL(10, 2) NOT NULL,
    quantity INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create a function to update the updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create trigger to automatically update updated_at column
CREATE TRIGGER update_products_updated_at BEFORE UPDATE
    ON products FOR EACH ROW EXECUTE PROCEDURE
    update_updated_at_column();

-- Insert some sample data
INSERT INTO products (name, price, quantity) VALUES
    ('Laptop', 1200.00, 10),
    ('Mouse', 25.00, 50),
    ('Keyboard', 75.00, 30);