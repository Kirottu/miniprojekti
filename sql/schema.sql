PRAGMA journal_mode=WAL;
PRAGMA foreign_keys=ON;

-- FIXME: How should authentication be handled?
DROP TABLE IF EXISTS users;
CREATE TABLE users(
    -- UUID
    id TEXT NOT NULL,
    name TEXT NOT NULL,
    PRIMARY KEY(id)  
);

-- "references" is reserved by SQL so this'll have to do
DROP TABLE IF EXISTS citations;
CREATE TABLE citations(
    -- UUID
    id TEXT NOT NULL,
    -- The owner of the reference
    user TEXT NOT NULL,
    -- Mandatory for every entry
    entrytype TEXT NOT NULL,
    citekey TEXT NOT NULL,
    -- BibTeX fields
    address TEXT,
    annote TEXT,
    booktitle TEXT,
    chapter TEXT,
    edition TEXT,
    howpublished TEXT,
    institution TEXT,
    journal TEXT,
    month TEXT,
    note TEXT,
    number TEXT,
    organization TEXT,
    pages TEXT,
    publisher TEXT,
    school TEXT,
    series TEXT,
    title TEXT,
    type TEXT,
    volume TEXT,
    year TEXT,

    -- Non-standard fields
    doi TEXT,
    issn TEXT,
    isbn TEXT,
    url TEXT,
    FOREIGN KEY(user) REFERENCES users(id) ON DELETE CASCADE,
    PRIMARY KEY(id)
);

DROP TABLE IF EXISTS authors;
CREATE TABLE authors(
    reference TEXT NOT NULL,
    author TEXT NOT NULL,
    FOREIGN KEY(reference) REFERENCES citations(id) ON DELETE CASCADE,
    PRIMARY KEY(reference, author)
);
