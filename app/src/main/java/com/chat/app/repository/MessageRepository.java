package com.chat.app.repository;

import com.chat.app.model.Message;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.List;

public interface MessageRepository extends JpaRepository<Message, Long> {

    // Exemple pour récupérer l'historique d'une conversation privée
    @Query("SELECT m FROM Message m WHERE (m.sender.id = :userId1 AND m.recipient.id = :userId2) OR (m.sender.id = :userId2 AND m.recipient.id = :userId1) ORDER BY m.timestamp ASC")
    List<Message> findConversationHistory(@Param("userId1") Long userId1, @Param("userId2") Long userId2);

    // Historique d'un salon public
    List<Message> findByChannelIdOrderByTimestampAsc(Long channelId);

    // Historique public générique (ni privé, ni salon)
    List<Message> findByRecipientIsNullAndChannelIsNullOrderByTimestampAsc();

    // Ajoutez d'autres méthodes si nécessaire (ex: pour des salons publics/groupes)
    // List<Message> findByChannelIdOrderByTimestampAsc(Long channelId); 
} 