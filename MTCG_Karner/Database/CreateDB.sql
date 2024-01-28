CREATE DATABASE MTCG_Data;


CREATE TABLE IF NOT EXISTS users (
    id              serial         PRIMARY KEY,
    username        VARCHAR(255)    NOT NULL UNIQUE,
    password        VARCHAR(255)    NOT NULL
);
ALTER TABLE users ADD COLUMN coins INTEGER DEFAULT 20;
ALTER TABLE users
    ADD COLUMN name VARCHAR(255),
    ADD COLUMN bio TEXT,
    ADD COLUMN image TEXT;
ALTER TABLE users
    ADD COLUMN wins INT DEFAULT 0,
    ADD COLUMN losses INT DEFAULT 0,
    ADD COLUMN games_played INT DEFAULT 0;
ALTER TABLE users ADD COLUMN elo INT DEFAULT 100;




CREATE TABLE IF NOT EXISTS cards (
                                     id              UUID            PRIMARY KEY,
                                     name            VARCHAR(255)    NOT NULL,
                                     damage          DOUBLE PRECISION NOT NULL
);
ALTER TABLE cards
    ADD COLUMN owner_id INT,
    ADD FOREIGN KEY (owner_id) REFERENCES users(id);


CREATE TABLE IF NOT EXISTS packages (
                                        id       SERIAL PRIMARY KEY,
                                        card_id1 UUID NOT NULL,
                                        card_id2 UUID NOT NULL,
                                        card_id3 UUID NOT NULL,
                                        card_id4 UUID NOT NULL,
                                        card_id5 UUID NOT NULL,
                                           FOREIGN KEY (card_id1) REFERENCES cards(id),
                                        FOREIGN KEY (card_id2) REFERENCES cards(id),
                                        FOREIGN KEY (card_id3) REFERENCES cards(id),
                                        FOREIGN KEY (card_id4) REFERENCES cards(id),
                                        FOREIGN KEY (card_id5) REFERENCES cards(id)
);

CREATE TABLE IF NOT EXISTS user_cards (
                                          user_id     INT             NOT NULL,
                                          card_id     UUID            NOT NULL,
                                          FOREIGN KEY (user_id) REFERENCES users(id),
                                          FOREIGN KEY (card_id) REFERENCES cards(id)
);

CREATE TABLE IF NOT EXISTS decks (
                                     deck_id   SERIAL PRIMARY KEY,
                                     user_id   INT,
                                     card_id1  UUID,
                                     card_id2  UUID,
                                     card_id3  UUID,
                                     card_id4  UUID,
                                     FOREIGN KEY (user_id) REFERENCES users(id),
                                     FOREIGN KEY (card_id1) REFERENCES cards(id),
                                     FOREIGN KEY (card_id2) REFERENCES cards(id),
                                     FOREIGN KEY (card_id3) REFERENCES cards(id),
                                     FOREIGN KEY (card_id4) REFERENCES cards(id)
);

SELECT COUNT(*) FROM cards WHERE cards.owner_id = 91;



--Delete <3
DELETE FROM decks;
DELETE FROM packages;
DELETE FROM cards;
Delete From Users;


