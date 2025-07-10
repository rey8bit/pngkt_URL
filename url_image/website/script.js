fetch('data/billboard_data.json') // Sesuaikan path-nya
    .then(response => response.json())
    .then(data => {
        console.log(data); // Data JSON akan ada di sini
        // Lakukan sesuatu dengan data, misalnya update tampilan
    })
    .catch(error => console.error('Error fetching JSON:', error));
