# lingvo--LanguageDetector

<a target="_blank" href="http://ldt.apphb.com/index.html">[ live demo ]</a>

<div style="padding: 20px">
                        <p>
						    Automatic identification of language - an important first step of automatic text processing process.
                            Wrong definition of language posts will lead to its incorrect handling.
                            If the problem seeming simplicity of its practical implementation is not so obvious.
                            This is particularly evident in the processing of short messages, such as the tweets.
                            <br>
						    Firstly, there is a set of related languages ​​that use alphabets close and similar language, such as Russian, Byelorussian, Ukrainian, Bulgarian, Serbian, and so forth.
                            In such cases, solutions based on simple statistics words, letters or combinations thereof, do not work well.
                            For example, a simple sentence, "Mom soap frame" on the Bulgarian will be <i> «shirt izmiva frame» </i>.
                            Two words of this sentence on the Bulgarian and Russian are, though in a somewhat different meaning.
                            The system can easily recognize this text as a Russian.
                            <br>
						    Secondly, the same spelling of words may occur in unrelated languages.
                            And if these words frequency, the percentage may be quite large.
                            For example, English is sufficient frequency preposition <i> «on» </i> in the Finnish language is a verb olla (be) in the third person singular, and, accordingly, the word at the same frequency.
                            Therefore, the proposal <i>"sauna on sijoitettu metsään"</i> system can recognize as english.
						    Conversely, the proposal <i>"sauna on wheels"</i>, as the Finnish.
                            </p><p>
						    The presented system is devoid of these shortcomings, and works quite well with the related languages.
						    <br>
						    The demo version can be identified as languages with Cyrillic alphabet and using the Latin alphabet.                            
						    <br>
						    The output shows the probability distribution of messages by language, normalized to 100%.
						    </p><p>
						    Accuracy depends on the definition of language group and the proximity of recognizable languages.
                            On the accuracy of an average of about 96-97%.
						    <br>
						    <b>The speed of the unit - about 10 MB/sec text in double-byte character set.</b>
                        </p>			
                    </div>
		    
# examples
<hr/>
<table><tr><td>
Захворванне абумоўлена губчастым перараджэннем галаўны мозг галаўнога мозга.У жывёл найбольш вылучаюць спангіяформную энцэфалапатыю буйной рагатай жывёлы т.зв. шаленства кароў ад мяса якіх адбываецца заражэнне людзей скрыты перыяд у людзей - гадоў.
</td></tr></table>
<table><tr><td>BE</td><td>belorussian / белорусский</td><td>98%</td></tr></table>
<hr/>
<table><tr><td>
Typing an @ symbol, followed by a username, will notify that person to come and view the comment. This is called an “@mention”, because you’re mentioning the individual. You can also @mention teams within an organization.
</td></tr></table>
<table><tr><td>  EN  </td><td>  english / английский  </td><td>  90%  </td></tr></table>
<hr/>
<table><tr><td>
Typing an @ symbol, followed by a username, will notify that person to come and view the comment. This is called an “@mention”, because you’re mentioning the individual. You can also @mention teams within an organization.
<br/><br/>
Захворванне абумоўлена губчастым перараджэннем галаўны мозг галаўнога мозга.У жывёл найбольш вылучаюць спангіяформную энцэфалапатыю буйной рагатай жывёлы т.зв. шаленства кароў ад мяса якіх адбываецца заражэнне людзей скрыты перыяд у людзей - гадоў.
</td></tr></table>
<table>
                            <tbody><tr><td>EN</td><td>english / английский</td><td>71%</td></tr><tr><td>BE</td><td>belorussian / белорусский</td><td>20%</td></tr></tbody>
                        </table>
<hr/>			

<table><tr><td>
<a href="https://fi.wikipedia.org/wiki/Lissabon" target="_blank">Lissabonin sijainti maapallon</a> koordinaatistossa on 38° 42' pohjoista leveyttä ja 9° 8' läntistä pituutta. Lissabon on Manner-Euroopan läntisin pääkaupunki (Islannin Reykjavik on koko Euroopan läntisin). Kaupunki sijaitsee Portugalin länsiosassa, Atlantin valtameren rannalla, Tejojoen suulla. Lissabonin pinta-ala 84,6 km². Toisin kuin monien muiden suurkaupunkien, Lissabonin rajat ovat kaupungin historiallisten rajojen mukaan vedetyt. Tämän takia itse Lissabonin ympärillä on monia hallinnollisia alueita, kuten Loures, Amadora ja Oeiras, jotka ovat osa Suur-Lissabonin aluetta.
</td></tr></table>
<table>
                            <tbody><tr><td>FI</td><td>finnish / финский</td><td>99%</td></tr></tbody>
                        </table>
<hr/>			
<table><tr><td>
<a href="https://pt.wikipedia.org/wiki/Hugo_Ch%C3%A1vez" target="_blank">Hugo Chávez Frías GColIH</a> (Sabaneta, 28 de julho de 1954  — Caracas, 5 de março de 2013) foi um político e militar venezuelano, tendo sido o 56.º presidente da Venezuela, governando por 14 anos desde 1999 até sua morte em 2013. Líder da Revolução Bolivariana, Chávez advogava a doutrina bolivarianista, promovendo o que denominava de socialismo do século XXI.[2] Chávez foi também um crítico do neoliberalismo e da política externa dos Estados Unidos.[3]Oficial militar de carreira, Chávez fundou o Movimento Quinta República, da esquerda política, depois de capitanear um golpe de estado mal-sucedido contra o governo de Carlos Andrés Pérez, em 1992.[4]
</td></tr></table>
<table>
                            <tbody><tr><td>PT</td><td>portuguese / португальский</td><td>74%</td></tr><tr><td>ES</td><td>spanish / испанский</td><td>24%</td></tr></tbody>
                        </table>
<hr/>			
